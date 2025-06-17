using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Зоогостиница_диплом_;

namespace Зоогостиница_диплом_
{

    public static class DatabaseHelper
    {
        public class Pet
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Breed { get; set; }
            public string Size { get; set; }
            public double Weight { get; set; }
            public int Age { get; set; }
            public string Photo { get; set; }
            public string BehaviorDescription { get; set; }
            public string Color { get; set; }
            public string Gender { get; set; }
            public string BirthDate { get; set; }
            public string OwnerName { get; set; }
            // Можно добавить другие поля по необходимости
        }

        private static string connectionString = "Server=localhost;Port=5434;Username=postgres;Password=1234;Database=diplom";
        public static Pet GetPetByCellNumber(int cellNumber)
        {
            
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT a.nickname, at.type_name, a.size, a.color, a.gender, a.birth_date, f.feeding_type
                FROM animals a
                JOIN animal_types at ON a.animal_type_id = at.id
                JOIN feeding_types f ON a.feeding_type_id = f.id
                JOIN bookings b ON b.animal_id = a.id
                WHERE b.id = @CellNumber"; 

                
                string petQuery = @"
                SELECT a.nickname, at.type_name, a.size, a.color, a.gender, a.birth_date
                FROM animals a
                JOIN animal_types at ON a.animal_type_id = at.id
                WHERE a.id = (
                    SELECT a2.id FROM animals a2
                    JOIN bookings b ON b.animal_id = a2.id
                    WHERE b.id = @CellNumber
                );";

                using (var cmd = new NpgsqlCommand(petQuery, conn))
                {
                    cmd.Parameters.AddWithValue("CellNumber", cellNumber);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Pet
                            {
                                Name = reader["nickname"].ToString(),
                                Breed = reader["type_name"].ToString(),
                                Size = reader["size"].ToString(),
                                Weight = 0, 
                                Age = (DateTime.Now - DateTime.Parse(reader["birth_date"].ToString())).Days / 365,
                                Photo = "default_photo.jpg" 
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}