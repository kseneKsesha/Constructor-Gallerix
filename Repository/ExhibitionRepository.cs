using System;
using System.Collections.Generic;
using Npgsql;
using Constructor_Gallerix.Models;

namespace Constructor_Gallerix.Repository
{
    public class ExhibitionRepository : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed = false;

        public ExhibitionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Exhibition> GetUserExhibitions(int userId)
        {
            var exhibitions = new List<Exhibition>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT exhibition_id, user_id, title, description, 
                     is_public, is_favorite, created_at, 
                     cover_image_data, template
                     FROM exhibitions 
                     WHERE user_id = @userId 
                     ORDER BY created_at DESC", connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            exhibitions.Add(new Exhibition
                            {
                                ExhibitionId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                Title = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                IsPublic = reader.GetBoolean(4),
                                IsFavorite = reader.GetBoolean(5),
                                CreatedAt = reader.GetDateTime(6),
                                CoverImageData = reader.IsDBNull(7) ? null : (byte[])reader[7],
                                Template = reader.GetString(8),
                                Pictures = new List<Picture>()
                            });
                        }
                    }
                }
            }
            return exhibitions;
        }

        public Exhibition GetExhibitionWithPictures(int exhibitionId)
        {
            Exhibition exhibition = null;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                // 1. Сначала получаем выставку
                using (var cmd = new NpgsqlCommand(
                    @"SELECT exhibition_id, user_id, title, description, 
                     is_public, is_favorite, created_at, 
                     cover_image_data, template
                     FROM exhibitions
                     WHERE exhibition_id = @exhibitionId", connection))
                {
                    cmd.Parameters.AddWithValue("@exhibitionId", exhibitionId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            exhibition = new Exhibition
                            {
                                ExhibitionId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                Title = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                IsPublic = reader.GetBoolean(4),
                                IsFavorite = reader.GetBoolean(5),
                                CreatedAt = reader.GetDateTime(6),
                                CoverImageData = reader.IsDBNull(7) ? null : (byte[])reader[7],
                                Template = reader.GetString(8)
                            };
                        }
                    }
                }

                // 2. Потом — картины для выставки
                if (exhibition != null)
                {
                    exhibition.Pictures = new List<Picture>();
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
                                exhibition.Pictures.Add(new Picture
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
            }
            return exhibition;
        }

        public void UpdateExhibition(Exhibition exhibition)
        {
            if (exhibition == null)
                throw new ArgumentNullException(nameof(exhibition));

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Обновление основной информации о выставке
                        using (var cmd = new NpgsqlCommand(
                            @"UPDATE exhibitions SET
                                title = @title,
                                description = @description,
                                is_public = @isPublic,
                                is_favorite = @isFavorite,
                                cover_image_data = @coverImageData,
                                template = @template
                            WHERE exhibition_id = @exhibitionId", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@title", exhibition.Title);
                            cmd.Parameters.AddWithValue("@description", exhibition.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@isPublic", exhibition.IsPublic);
                            cmd.Parameters.AddWithValue("@isFavorite", exhibition.IsFavorite);
                            cmd.Parameters.AddWithValue("@coverImageData", exhibition.CoverImageData ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@template", exhibition.Template);
                            cmd.Parameters.AddWithValue("@exhibitionId", exhibition.ExhibitionId);

                            cmd.ExecuteNonQuery();
                        }

                        // Обновление картин
                        UpdatePicturesForExhibition(exhibition, connection, transaction);

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

        private void UpdatePicturesForExhibition(Exhibition exhibition, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            // Удаляем старые картины
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM pictures WHERE exhibition_id = @exhibitionId", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@exhibitionId", exhibition.ExhibitionId);
                cmd.ExecuteNonQuery();
            }

            // Добавляем новые картины
            if (exhibition.Pictures != null && exhibition.Pictures.Count > 0)
            {
                foreach (var picture in exhibition.Pictures)
                {
                    using (var cmd = new NpgsqlCommand(
                        @"INSERT INTO pictures (
                            exhibition_id, title, author, year, 
                            description, expert_comment, image_data, orientation
                        ) VALUES (
                            @exhibitionId, @title, @author, @year, 
                            @description, @expertComment, @imageData, @orientation
                        )", connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@exhibitionId", exhibition.ExhibitionId);
                        cmd.Parameters.AddWithValue("@title", picture.Title ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@author", picture.Author ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@year", picture.Year);
                        cmd.Parameters.AddWithValue("@description", picture.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@expertComment", picture.ExpertComment ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@imageData", picture.ImageData ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@orientation", picture.Orientation ?? "Horizontal");

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public int SaveExhibition(Exhibition exhibition)
        {
            if (exhibition == null)
                throw new ArgumentNullException(nameof(exhibition));

            if (exhibition.ExhibitionId > 0)
            {
                UpdateExhibition(exhibition);
                return exhibition.ExhibitionId;
            }

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int exhibitionId;

                        // Сохраняем основную информацию о выставке
                        using (var cmd = new NpgsqlCommand(
                            @"INSERT INTO exhibitions (
                                user_id, title, description, is_public, 
                                is_favorite, created_at, cover_image_data, template
                            ) VALUES (
                                @userId, @title, @description, @isPublic, 
                                @isFavorite, @createdAt, @coverImageData, @template
                            ) RETURNING exhibition_id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@userId", exhibition.UserId);
                            cmd.Parameters.AddWithValue("@title", exhibition.Title);
                            cmd.Parameters.AddWithValue("@description", exhibition.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@isPublic", exhibition.IsPublic);
                            cmd.Parameters.AddWithValue("@isFavorite", exhibition.IsFavorite);
                            cmd.Parameters.AddWithValue("@createdAt", exhibition.CreatedAt);
                            cmd.Parameters.AddWithValue("@coverImageData", exhibition.CoverImageData ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@template", exhibition.Template);

                            exhibitionId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Сохраняем картины
                        if (exhibition.Pictures != null && exhibition.Pictures.Count > 0)
                        {
                            foreach (var picture in exhibition.Pictures)
                            {
                                using (var cmd = new NpgsqlCommand(
                                    @"INSERT INTO pictures (
                                        exhibition_id, title, author, year, 
                                        description, expert_comment, image_data, orientation
                                    ) VALUES (
                                        @exhibitionId, @title, @author, @year, 
                                        @description, @expertComment, @imageData, @orientation
                                    )", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@exhibitionId", exhibitionId);
                                    cmd.Parameters.AddWithValue("@title", picture.Title ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@author", picture.Author ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@year", picture.Year);
                                    cmd.Parameters.AddWithValue("@description", picture.Description ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@expertComment", picture.ExpertComment ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@imageData", picture.ImageData ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@orientation", picture.Orientation ?? "Horizontal");

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                        return exhibitionId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void DeleteExhibition(int exhibitionId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Удаляем картины выставки
                        using (var cmd = new NpgsqlCommand(
                            "DELETE FROM pictures WHERE exhibition_id = @id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", exhibitionId);
                            cmd.ExecuteNonQuery();
                        }

                        // Удаляем саму выставку
                        using (var cmd = new NpgsqlCommand(
                            "DELETE FROM exhibitions WHERE exhibition_id = @id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", exhibitionId);
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
