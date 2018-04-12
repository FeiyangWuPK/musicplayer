using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using QuartzTypeLib;
using System.ComponentModel;
using System.Windows.Media;


namespace JustAnotherMusicPlayer
{
    public delegate void SongChangedEventHandler(object sender, EventArgs e);
    
    public class MusicPlayer 
    {
        private Playlist playlist;
        //private IMediaControl controller;
        //private IMediaPosition position;
        public MediaPlayer control = new MediaPlayer();
        public event SongChangedEventHandler SongChanged;

        public MusicPlayer() {}

        public MusicPlayer(Playlist playlist)
        {
            LoadPlaylist(playlist);

            if (this.playlist.CurrentSong.Path != "Unknown")
            {
                LoadSong(this.playlist.CurrentSong.Path);
            }

            this.playlist.SongAdded += new SongAddedEventHandler(playlist_SongAdded);
        }

        void playlist_SongAdded(object sender, EventArgs e)
        {
            if (this.playlist.Songs.Count == 1)
            {
                LoadSong(this.playlist.CurrentSong.Path);
            }
        }

        public void LoadPlaylist(Playlist playlist)
        {
            this.playlist = playlist;
        }

        private void LoadSong(string path)
        {
            FilgraphManager filterManager = new FilgraphManager();
            filterManager.RenderFile(path);
           // this.controller = filterManager as IMediaControl;
           // this.position = filterManager as IMediaPosition;
            Uri current = new Uri(this.playlist.CurrentSong.Path, UriKind.Relative);
            this.control.Open(current);
        }

        public void Play()
        {
            if (IsSongChanged)
            {
                this.LoadSong(this.playlist.CurrentSong.Path);
            }

            //this.controller.Run();
           
            this.control.Play();
            IsPlaying = true;
            SongChanged(this, EventArgs.Empty);
        }

        public void Pause()
        {
            this.control.Pause();
            IsPlaying = false;
            IsSongChanged = false;
        }

        public void Stop()
        {
            if (IsPlaying)
            {
                this.control.Stop();
                this.control.Position = TimeSpan.Zero;
                IsPlaying = false;
                IsSongChanged = false;
            }
        }

        public void PlayNext()
        {
            this.Stop();
            this.playlist.Next();
            IsSongChanged = true;
            this.Play();
            SongChanged(this, EventArgs.Empty);
        }

        public void PlayPrevious()
        {
            this.Stop();
            this.playlist.Previous();
            IsSongChanged = true;
            this.Play();
            SongChanged(this, EventArgs.Empty);
        }
		
		public Playlist Playlist
		{
			get
			{
				return this.playlist;
			}
		}

        public double CurrentPosition
        {
            get { return control.Position.TotalSeconds; }
            set
            {   

                control.Position =TimeSpan.FromSeconds(value) ;
            }
        }

        public void PlaySong(int index)
        {
            this.Stop();
            this.playlist.SelectSongIndex(index);
            IsSongChanged = true;
            this.Play();
        }

        public void ChangeVolume (double value)
        {
           
        }
              
        public bool IsPlaying { get; set; }
        public bool IsSongChanged { get; private set; }

       

    }
}
