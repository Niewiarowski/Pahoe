using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Pahoe.Search
{
    public sealed class SearchResult
    {
        public LoadType LoadType { get; }

        public PlaylistInfo PlaylistInfo { get; }

        public IReadOnlyList<LavalinkTrack> Tracks { get; }

        internal SearchResult(LoadType loadType, PlaylistInfo playlistInfo, List<LavalinkTrack> tracks)
        {
            LoadType = loadType;
            PlaylistInfo = playlistInfo;
            Tracks = tracks.AsReadOnly();
        }

        internal static SearchResult FromStream(Stream stream)
        {
            Span<byte> data = stackalloc byte[16384];
            int bytesRead = 0;

            while (true)
            {
                int tmpBytesRead = stream.Read(data.Slice(bytesRead));
                if (tmpBytesRead == 0)
                    break;

                bytesRead += tmpBytesRead;
                if (bytesRead == data.Length && stream.CanRead)
                {
                    Span<byte> newData = new byte[data.Length * 2];
                    data.CopyTo(newData);
                    data = newData;
                }
            }

            LoadType loadType = default;
            List<LavalinkTrack> tracks = new List<LavalinkTrack>();
            PlaylistInfo playlistInfo = null;
            Utf8JsonReader reader = new Utf8JsonReader(data.Slice(0, bytesRead));

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("exception"))
                    {
                        string message = null;
                        string severity = null;

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                throw new SearchException(message, severity);
                            }
                            else if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                reader.Read();
                                if (reader.ValueTextEquals("message"))
                                    message = reader.GetString();
                                else if (reader.ValueTextEquals("severity"))
                                    severity = reader.GetString();
                            }
                        }
                    }

                    if (reader.ValueTextEquals("loadType"))
                    {
                        reader.Read();
                        loadType = (LoadType) reader.ValueSpan[0];
                    }
                    else if (reader.ValueTextEquals("playlistInfo"))
                    {
                        string name = null;
                        int selectedTrack = default;

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                break;
                            }
                            else if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                if (reader.ValueTextEquals("name"))
                                    name = reader.GetString();
                                else if (reader.ValueTextEquals("selectedTrack"))
                                    selectedTrack = reader.GetInt32();
                            }
                        }

                        playlistInfo = new PlaylistInfo(name, selectedTrack);
                    }
                    else if (reader.ValueTextEquals("tracks"))
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                            {
                                break;
                            }
                            else if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                var track = new LavalinkTrack();

                                while (reader.Read())
                                {
                                    if (reader.TokenType == JsonTokenType.EndObject)
                                    {
                                        break;
                                    }
                                    else if (reader.TokenType == JsonTokenType.PropertyName)
                                    {
                                        if (reader.ValueTextEquals("track"))
                                        {
                                            reader.Read();
                                            track.Hash = reader.GetString();
                                        }
                                    }
                                    else if (reader.TokenType == JsonTokenType.StartObject)
                                    {
                                        while (reader.Read())
                                        {
                                            if (reader.TokenType == JsonTokenType.EndObject)
                                            {
                                                break;
                                            }
                                            else if (reader.TokenType == JsonTokenType.PropertyName)
                                            {
                                                static bool equals(ReadOnlySpan<byte> bytes, ReadOnlySpan<char> str)
                                                {
                                                    for (int i = 0; i < bytes.Length; i++)
                                                        if (bytes[i] != (byte) str[i])
                                                            return false;

                                                    return true;
                                                }

                                                ReadOnlySpan<byte> bytes = reader.ValueSpan;
                                                reader.Skip();

                                                if (equals(bytes, "identifier"))
                                                    track.Identifier = reader.GetString();
                                                else if (equals(bytes, "isSeekable"))
                                                    track.IsSeekable = reader.GetBoolean();
                                                else if (equals(bytes, "author"))
                                                    track.Author = reader.GetString();
                                                else if (equals(bytes, "length"))
                                                    track.Length = TimeSpan.FromMilliseconds(reader.GetDouble());
                                                else if (equals(bytes, "isStream"))
                                                    track.IsStream = reader.GetBoolean();
                                                else if (equals(bytes, "title"))
                                                    track.Title = reader.GetString();
                                                else if (equals(bytes, "uri"))
                                                    track.Uri = reader.GetString();
                                            }
                                        }
                                    }
                                }

                                tracks.Add(track);
                            }
                        }
                    }
                }
            }

            return new SearchResult(loadType, playlistInfo, tracks);
        }
    }
}
