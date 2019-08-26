using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Pahoe.Search
{
    public sealed class SearchResult
    {
        public LoadType LoadType { get; internal set; }
        public readonly PlaylistInfo PlaylistInfo;
        public List<LavalinkTrack> Tracks { get; internal set; }
        public readonly SearchException Exception;

        internal SearchResult(LoadType loadType, ref PlaylistInfo playlistInfo, List<LavalinkTrack> tracks, ref SearchException exception)
        {
            LoadType = loadType;
            PlaylistInfo = playlistInfo;
            Tracks = tracks;
            Exception = exception;
        }

        internal static SearchResult FromStream(Stream stream)
        {
            Span<byte> data = stackalloc byte[16384];
            int bytesRead = 0;

            do
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
            while (true);

            //Console.WriteLine(Encoding.UTF8.GetString(data.Slice(0, bytesRead)));

            LoadType loadType = default;
            List<LavalinkTrack> tracks = new List<LavalinkTrack>();
            PlaylistInfo playlistInfo = new PlaylistInfo();
            SearchException searchException = default;

            Utf8JsonReader reader = new Utf8JsonReader(data.Slice(0, bytesRead));

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("loadType"))
                    {
                        reader.Read();

                        if (reader.ValueTextEquals("TRACK_LOADED"))
                            loadType = LoadType.TrackLoaded;
                        else if (reader.ValueTextEquals("PLAYLIST_LOADED"))
                            loadType = LoadType.PlaylistLoaded;
                        else if (reader.ValueTextEquals("SEARCH_RESULT"))
                            loadType = LoadType.SearchResult;
                        else
                            loadType = LoadType.LoadFailed;
                    }
                    else if (reader.ValueTextEquals("playlistInfo"))
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                if (reader.ValueTextEquals("name"))
                                    playlistInfo.Name = reader.GetString();
                                else if (reader.ValueTextEquals("selectedTrack"))
                                    playlistInfo.SelectedTrack = reader.GetInt32();
                            }
                            else if (reader.TokenType == JsonTokenType.EndObject)
                                break;
                        }
                    }
                    else if (reader.ValueTextEquals("tracks"))
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                                break;
                            else if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                LavalinkTrack track = new LavalinkTrack();

                                while (reader.Read())
                                {
                                    if (reader.TokenType == JsonTokenType.EndObject)
                                        break;
                                    if (reader.TokenType == JsonTokenType.PropertyName)
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
                                                break;
                                            else if (reader.TokenType == JsonTokenType.PropertyName)
                                            {
                                                static bool equals(ReadOnlySpan<byte> bytes, ReadOnlySpan<char> str)
                                                {
                                                    Span<byte> strBytes = stackalloc byte[str.Length];
                                                    Encoding.ASCII.GetBytes(str, strBytes);
                                                    return strBytes.SequenceEqual(bytes);
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
                    else if (reader.ValueTextEquals("exception"))
                    {
                        string message = null;
                        string severity = null;

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                searchException = new SearchException(message, severity);
                                break;
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
                }
            }

            return new SearchResult(loadType, ref playlistInfo, tracks, ref searchException);
        }
    }
}
