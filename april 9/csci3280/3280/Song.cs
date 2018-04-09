using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using TagLib.Id3v2;
using System.ComponentModel;

namespace JustAnotherMusicPlayer
{
    public class Song 
    {
        private string title;
        private string artist;
        private string album;
        private string genre;
        private int year;
        private string duration;
        private string path;
        private const string UNKNOWN = "Unknown";

        public Song()
        {
            Title = UNKNOWN;
            Artist = UNKNOWN;
            Album = UNKNOWN;
            Genre = UNKNOWN;
            Year = 0;
            Duration = "0:00";
            path = UNKNOWN;
        }

        public Song(string title, string artist, string album = UNKNOWN, string genre = UNKNOWN,
                    int year = 0, string duration = "0:00", string path = UNKNOWN)
        {
            Title = title;
            Artist = artist;
            Album = album;
            Genre = Genre;
            Year = year;
            Duration = duration;
            Path = path;
        }

        public Song(string filePath)
        {
            TagLib.File mp3File = TagLib.File.Create(filePath);
            Title = mp3File.Tag.Title;
            Artist = mp3File.Tag.FirstPerformer;
            Album = mp3File.Tag.Album;
            Genre = mp3File.Tag.FirstGenre;
            Year = (int)mp3File.Tag.Year;
            TimeSpan songDuration = mp3File.Properties.Duration;
            Duration = String.Format("{0}:{1}", songDuration.Minutes, songDuration.Seconds);
            Path = filePath;
        }

        public string Title
        {
            get { return this.title; }
            set
            {
                if(String.IsNullOrEmpty(value))
                {
                    this.title = UNKNOWN;
                }

                this.title = value;
            }
        }

        public string Artist
        {
            get { return this.artist; }
            set
            {
                if(String.IsNullOrEmpty(value))
                {
                    this.artist = UNKNOWN;
                }

                this.artist = value;
            }
        }

        public string Album
        {
            get { return this.album; }
            set
            {
                if(String.IsNullOrEmpty(value))
                {
                    this.album = UNKNOWN;
                }

                this.album = value;
            }
        }
        
        public string Genre
        {
            get { return this.genre; }
            set
            {
                if(String.IsNullOrEmpty(value))
                {
                    this.genre = UNKNOWN;
                }

                this.genre = value;
            }
        }

        public int Year
        {
            get { return this.year; }
            set { this.year = value; }
        }

        public string Duration
        {
            get { return this.duration; }
            set
            {
                if(String.IsNullOrEmpty(value))
                {
                    this.duration = "0:00";
                }
                
                this.duration = value;
            }
        }

        public int DurationInSeconds
        {
            get 
            { 
                int minutes = Int32.Parse(this.duration.Split(':')[0]);
                int seconds = Int32.Parse(this.duration.Split(':')[1]);
                return minutes*60 + seconds;
            }
        }

        public string Path
        {
            get { return this.path; }
            set { this.path = value; }
        }
    }
}
