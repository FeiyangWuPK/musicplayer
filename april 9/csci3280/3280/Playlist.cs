using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace JustAnotherMusicPlayer
{
    public delegate void SongAddedEventHandler(object sender, EventArgs e);
    public class Playlist
    {        
        private List<Song> songs;
        private Song currentSong;

        public event SongAddedEventHandler SongAdded;
        
        public Playlist()
        {
            this.songs = new List<Song>();
            this.currentSong = new Song();
        }

        public Playlist(string directoryPath)
        {
            this.songs = new List<Song>();
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            if(dir.GetFiles().Length != 0)
            {
                foreach (var file in dir.GetFiles())
                {
                    if(file.Extension == ".mp3")
                    {
                        this.songs.Add(new Song(file.FullName));
                    }
                }
            }

            if(this.songs.Count > 0)
            {
                this.currentSong = this.songs[0];
            }
        }

        public Song CurrentSong
        {
            get { return this.currentSong; }
        }

        public void Next()
        {
            this.currentSong = NextSong;
        }

        public void Previous()
        {
            this.currentSong = PreviousSong;
        }

        public Song NextSong
        {
            get
            {
                int currIndex = this.songs.IndexOf(this.currentSong);
                if (currIndex < this.songs.Count - 1)
                {
                    return this.songs[currIndex + 1];
                }

                if(currIndex == this.songs.Count - 1)
                {
                    return this.songs.First();
                }
                else
                {
                    return null;
                }
            }
        }

        public Song PreviousSong
        {
            get
            {
                int currIndex = this.songs.IndexOf(this.currentSong);
                if (currIndex > 0)
                {
                    return this.songs[currIndex - 1];
                }

                if(currIndex == 0)
                {
                    return this.songs.Last();
                }
                else
                {
                    return null;
                }
            }
        }

        public List<Song> Songs
        {
            get { return this.songs; }
        }

        public void SelectSongIndex(int index)
        {
            this.currentSong = this.songs[index];
        }

        public void AddSong(Song song)
        {
            this.songs.Add(song);
            if (this.currentSong.Path == "Unknown")
            {
                this.currentSong = song;
            }
            SongAdded(this, EventArgs.Empty);
        }
    }
}
