using System;
using System.Collections.Generic;
using Npgsql;
using Constructor_Gallerix.Models;

namespace Constructor_Gallerix.Repository
{
    public class PictureRepository : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed = false;

        public PictureRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Picture> GetPicturesByExhibition(int exhibitionId)
        {
            var pictures = new List<Picture>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT picture_id, exhibition_id, title, author, year, 
                     description, expert_comment, image_data, orientation
                     FROM pictures WHERE exhibition_id = @exhibitionId", connection))
                {
                    cmd.Parameters.AddWithValue("@exhibitionId", exhibitionId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pictures.Add(new Picture
                            {
                                Id = reader.GetInt32(0),
                                ExhibitionId = reader.GetInt32(1),
                                Title = reader.IsDBNull(2) ? "Без названия" : reader.GetString(2),
                                Author = reader.IsDBNull(3) ? "Неизвестный автор" : reader.GetString(3),
                                Year = reader.IsDBNull(4) ? DateTime.Now.Year : reader.GetInt32(4),
                                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ExpertComment = reader.IsDBNull(6) ? null : reader.GetString(6),
                                ImageData = reader.IsDBNull(7) ? null : (byte[])reader[7],
                                Orientation = reader.IsDBNull(8) ? "Horizontal" : reader.GetString(8)
                            });
                        }
                    }
                }
            }
            return pictures;
        }

        public int SavePicture(Picture picture)
        {
            if (picture == null)
                throw new ArgumentNullException(nameof(picture));

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int pictureId;
                        using (var cmd = new NpgsqlCommand(
                            @"INSERT INTO pictures (
                                exhibition_id, title, author, year, 
                                description, expert_comment, image_data, orientation
                            ) VALUES (
                                @exhibitionId, @title, @author, @year, 
                                @description, @expertComment, @imageData, @orientation
                            ) RETURNING picture_id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@exhibitionId", picture.ExhibitionId);
                            cmd.Parameters.AddWithValue("@title", picture.Title ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@author", picture.Author ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@year", picture.Year);
                            cmd.Parameters.AddWithValue("@description", picture.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@expertComment", picture.ExpertComment ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@imageData", picture.ImageData ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@orientation", picture.Orientation ?? "Horizontal");

                            pictureId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        transaction.Commit();
                        return pictureId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void UpdatePicture(Picture picture)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new NpgsqlCommand(
                            @"UPDATE pictures SET
                                title = @title,
                                author = @author,
                                year = @year,
                                description = @description,
                                expert_comment = @expertComment,
                                image_data = @imageData,
                                orientation = @orientation
                            WHERE picture_id = @id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", picture.Id);
                            cmd.Parameters.AddWithValue("@title", picture.Title ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@author", picture.Author ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@year", picture.Year);
                            cmd.Parameters.AddWithValue("@description", picture.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@expertComment", picture.ExpertComment ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@imageData", picture.ImageData ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@orientation", picture.Orientation ?? "Horizontal");

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void DeletePicturesByExhibition(int exhibitionId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new NpgsqlCommand(
                            "DELETE FROM pictures WHERE exhibition_id = @exhibitionId", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@exhibitionId", exhibitionId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void DeletePicture(int pictureId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new NpgsqlCommand(
                            "DELETE FROM pictures WHERE picture_id = @id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", pictureId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
