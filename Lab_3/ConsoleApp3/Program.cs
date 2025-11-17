using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System.Collections.Generic;

class Program
{
    const int PORT = 5000;
    const string DB_PATH = "tickets.db";

    static void Main(string[] args)
    {
        SQLitePCL.Batteries_V2.Init();
        Console.OutputEncoding = Encoding.UTF8;

        EnsureDatabase();

        TcpListener listener = new TcpListener(IPAddress.Any, PORT);
        listener.Start();
        Console.WriteLine("[Server] Listening on port " + PORT);

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Task.Run(() => HandleClient(client));
        }
    }

    static void EnsureDatabase()
    {
        if (!File.Exists(DB_PATH))
        {
            Console.WriteLine("[DB] Creating new DB...");

            using var con = new SqliteConnection($"Data Source={DB_PATH}");
            con.Open();

            using var cmd = con.CreateCommand(); // cmd доступний усього цього блоку

            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS tickets(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    from_city TEXT NOT NULL,
    to_city TEXT NOT NULL,
    date TEXT NOT NULL,
    time TEXT NOT NULL,
    seat INTEGER,
    price REAL
);

CREATE TABLE IF NOT EXISTS users(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT,
    phone TEXT
);

CREATE TABLE IF NOT EXISTS bookings(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ticket_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    date TEXT NOT NULL,
    FOREIGN KEY(ticket_id) REFERENCES tickets(id),
    FOREIGN KEY(user_id) REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS routes(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    from_city TEXT NOT NULL,
    to_city TEXT NOT NULL,
    distance_km REAL,
    duration TEXT
);
";

            cmd.ExecuteNonQuery();

            // Додатково можна вставити початкові дані:
            cmd.CommandText = @"
INSERT INTO tickets(from_city, to_city, date, time, seat, price) VALUES
('Львів','Київ','2025-12-01','08:00',12,199.5),
('Львів','Київ','2025-12-01','12:30',5,249.0),
('Київ','Львів','2025-12-02','09:45',7,210.0);

INSERT INTO users(name,email,phone) VALUES
('Іван Іванов','ivan@gmail.com','380501234567'),
('Марія Петрова','maria@gmail.com','380671234567');

INSERT INTO routes(from_city,to_city,distance_km,duration) VALUES
('Львів','Київ',540,'07:30'),
('Львів','Одеса',740,'09:50');
";
            cmd.ExecuteNonQuery();

        }
        else
        {
            Console.WriteLine("[DB] DB exists");
        }

    }

    static void HandleClient(TcpClient client)
    {
        try
        {
            using var c = client;
            var stream = c.GetStream();

            byte[] buffer = new byte[4096];
            int read = stream.Read(buffer, 0, buffer.Length);

            string request = Encoding.UTF8.GetString(buffer, 0, read).Trim();
            Console.WriteLine("[Server] Received: " + request);

            string response = Process(request);

            byte[] respBytes = Encoding.UTF8.GetBytes(response);
            stream.Write(respBytes, 0, respBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Server] Error: " + ex.Message);
        }
    }

    static string Process(string cmd)
    {
        if (cmd == "GET_ALL")
        {
            var list = GetAll();
            return JsonConvert.SerializeObject(list) + "\n";
        }

        if (cmd.StartsWith("SEARCH"))
        {
            var p = cmd.Split(' ');
            if (p.Length < 3) return "ERROR SEARCH <from> <to>\n";
            return Search(p[1], p[2]);
        }

        if (cmd.StartsWith("BOOK"))
        {
            var p = cmd.Split(' ');
            if (p.Length < 2) return "ERROR BOOK <id>\n";

            if (!int.TryParse(p[1], out int id)) return "ERROR\n";
            return Book(id);
        }

        if (cmd.StartsWith("ADD"))
        {
            string json = cmd.Substring(4);
            return Add(json);
        }

        return "ERROR unknown command\n";
    }

    static List<Ticket> GetAll()
    {
        var list = new List<Ticket>();

        using var con = new SqliteConnection($"Data Source={DB_PATH}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT * FROM tickets";

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Ticket
            {
                Id = Convert.ToInt32(r["id"]),
                From = r["from_city"].ToString(),
                To = r["to_city"].ToString(),
                Date = r["date"].ToString(),
                Time = r["time"].ToString(),
                Seat = Convert.ToInt32(r["seat"]),
                Price = Convert.ToDecimal(r["price"])
            });
        }
        return list;
    }

    static string Search(string from, string to)
    {
        StringBuilder sb = new();

        using var con = new SqliteConnection($"Data Source={DB_PATH}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT * FROM tickets WHERE from_city=@f AND to_city=@t";
        cmd.Parameters.AddWithValue("@f", from);
        cmd.Parameters.AddWithValue("@t", to);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            sb.Append(
                r["id"] + ";" +
                r["from_city"] + ";" +
                r["to_city"] + ";" +
                r["date"] + ";" +
                r["time"] + ";" +
                r["seat"] + ";" +
                r["price"] + "\n"
            );
        }

        return sb.Length == 0 ? "NO_RESULTS\n" : sb.ToString();
    }

    static string Book(int id)
    {
        using var con = new SqliteConnection($"Data Source={DB_PATH}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM tickets WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);

        int rows = cmd.ExecuteNonQuery();

        return rows > 0 ? "BOOKED_OK\n" : "BOOK_FAILED\n";
    }

    static string Add(string json)
    {
        try
        {
            var t = JsonConvert.DeserializeObject<Ticket>(json);
            if (t == null) return "ERROR json\n";

            using var con = new SqliteConnection($"Data Source={DB_PATH}");
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText =
                "INSERT INTO tickets(from_city,to_city,date,time,seat,price) VALUES(@f,@t,@d,@ti,@s,@p)";

            cmd.Parameters.AddWithValue("@f", t.From);
            cmd.Parameters.AddWithValue("@t", t.To);
            cmd.Parameters.AddWithValue("@d", t.Date);
            cmd.Parameters.AddWithValue("@ti", t.Time);
            cmd.Parameters.AddWithValue("@s", t.Seat);
            cmd.Parameters.AddWithValue("@p", t.Price);

            cmd.ExecuteNonQuery();
            return "ADD_OK\n";
        }
        catch (Exception ex)
        {
            return "ERROR " + ex.Message + "\n";
        }
    }
}
