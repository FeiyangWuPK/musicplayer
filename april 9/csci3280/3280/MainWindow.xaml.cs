using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;


using System.Windows.Input;
using System.Windows.Media;

using System.Windows.Threading;
using Timer = System.Timers.Timer;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JustAnotherMusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class ProcessInfo
        {
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Length { get; set; }
        }

        public static IPAddress localAddr;
        public static TcpClient tcpclnt = new TcpClient();
        public MusicPlayer player = new MusicPlayer();
        Playlist playlist = new Playlist();
        private DispatcherTimer timer = new DispatcherTimer();
        public MediaPlayer playercopy = new MediaPlayer();
        public List<ProcessInfo> processes = new List<ProcessInfo>();
        public MainWindow()
        {
            Thread tcpthread1 = new Thread(() => TCPlisten(13000));
            Thread tcpthread2 = new Thread(() => TCPlisten(13001));
            Thread tcpthread3 = new Thread(() => TCPlisten(13002));
            tcpthread1.Start();
            tcpthread2.Start();
            tcpthread3.Start();

            InitializeComponent();
            try
            {
                lstSongs.ItemsSource = this.playlist.Songs;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace, ex.Message);
            }

            player = new MusicPlayer(playlist);
            player.SongChanged += new SongChangedEventHandler(player_SongChanged);
            ReadSongs();
            //SetBindings();
            ReadData();

        }

        void player_SongChanged(object sender, EventArgs e)
        {
            timer.IsEnabled = false;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.IsEnabled = true;
            volumeSlider.Value = 0.5;
            SetBindings();
            LoadLyrics();
        }
        void ReadSongs()
        {
            Song song1 = new Song("songs/Imagine Dragons - Demons [mqms2].mp3");
            this.playlist.AddSong(song1);
            Song song2 = new Song("songs/Imagine Dragons - Radioactive [mqms2].mp3");
            this.playlist.AddSong(song2);
            lstSongs.Items.Refresh();

        }

        void timer_Tick(object sender, EventArgs e)
        {
            this.sliderSongProgress.Value++;
            lblCurrentPosition.Text = FormatSeconds(sliderSongProgress.Value);
        }

        private void btnPrevious_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.player.PlayPrevious();
            this.btnPlay.Visibility = Visibility.Collapsed;
            this.btnPause.Visibility = Visibility.Visible;
        }

        private void btnPlay_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.player.Play();
            this.btnPlay.Visibility = Visibility.Collapsed;
            this.btnPause.Visibility = Visibility.Visible;
        }

        private void btnNext_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.player.PlayNext();
            this.btnPlay.Visibility = Visibility.Collapsed;
            this.btnPause.Visibility = Visibility.Visible;
        }

        private void btnPause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.player.Pause();
            this.btnPlay.Visibility = Visibility.Visible;
            this.btnPause.Visibility = Visibility.Collapsed;
            timer.Stop();
        }

        private void SetBindings()
        {
            lblTitle.Text = "Title: " + player.Playlist.CurrentSong.Title;
            lblArtist.Text = "Artist: " + player.Playlist.CurrentSong.Artist;
            lblAlbum.Text = "Album: " + player.Playlist.CurrentSong.Album;
            sliderSongProgress.Maximum = player.Playlist.CurrentSong.DurationInSeconds;
            sliderSongProgress.SelectionEnd = player.Playlist.CurrentSong.DurationInSeconds;
            if (player.IsSongChanged)
            {
                sliderSongProgress.Value = 0;
            }

            lblDuration.Text = player.Playlist.CurrentSong.Duration;
        }

        private void sliderSongProgress_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            player.CurrentPosition = sliderSongProgress.Value;
            // MessageBox.Show(sliderSongProgress.Value.ToString());
        }

        private void lstSongs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            player.PlaySong(this.lstSongs.SelectedIndex);
            this.btnPlay.Visibility = Visibility.Collapsed;
            this.btnPause.Visibility = Visibility.Visible;
        }

        private string FormatSeconds(double totalSeconds)
        {
            int minutes = (int)totalSeconds / 60;
            int seconds = (int)totalSeconds % 60;
            if (seconds < 10)
            {
                return string.Format("{0}:0{1}", minutes, seconds);
            }
            else
            {
                return string.Format("{0}:{1}", minutes, seconds);
            }
        }

        private void btnAddSong_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MP3 files(*.mp3)| *.mp3 | WAV Files | *.wav | All Files | *.* ";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                try
                {

                    Song song = new Song(openFileDialog.FileName);
                    this.playlist.AddSong(song);
                    lstSongs.Items.Refresh();
                    SetBindings();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Songs.Items.Clear();
            label.Visibility = Visibility.Visible;
            box.Visibility = Visibility.Visible;
            b1.Visibility = Visibility.Visible;
            b2.Visibility = Visibility.Visible;
            lyc.Visibility = Visibility.Hidden;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            //Songs.Items.Clear();
            label2.Visibility = Visibility.Visible;
            label3.Visibility = Visibility.Visible;
            box.Visibility = Visibility.Visible;
            box2.Visibility = Visibility.Visible;
            b3.Visibility = Visibility.Visible;
            b3.Content = "connect";
            b4.Visibility = Visibility.Visible;
            lyc.Visibility = Visibility.Hidden;

        }

        List<string> prints = new List<string>();
        public void ReadData()
        {

            string line1;

            StreamReader reader1 = new StreamReader("database/database.txt");
            while ((line1 = reader1.ReadLine()) != null)
            {

                ProcessInfo newInfo = new ProcessInfo();

                string[] splitlist = line1.Split(',');
                newInfo.Title = splitlist[0];
                newInfo.Artist = splitlist[1];
                newInfo.Album = splitlist[2];
                newInfo.Length = splitlist[3];
                processes.Add(newInfo);

            }
            reader1.Close();

        }

        public void LoadLyrics()
        {
            lyrics.Items.Clear();
            string line;
            int index = 0;
            StreamReader reader = new StreamReader("lyrics/" + player.Playlist.CurrentSong.Title + ".txt");
            while ((line = reader.ReadLine()) != null)
            {

                ListBoxItem itm = new ListBoxItem();
                itm.Content = line;
                lyrics.Items.Insert(index, itm);
                index++;
            }
        }
        List<string> record = new List<string>();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            record.Clear();
            Songs.Items.Clear();
            Musicbar.Visibility = Visibility.Visible;
            Songs.Visibility = Visibility.Visible;
            int index = 0;
            for (int i = 0; i < processes.Count; i++)
            {
                if (processes[i].Title == box.Text || processes[i].Artist == box.Text || processes[i].Album == box.Text)
                {
                    //MessageBox.Show(a.Artist);                     
                    ListBoxItem itm = new ListBoxItem();
                    record.Add(processes[i].Title);
                    itm.Content = processes[i].Title.PadRight(23) + processes[i].Artist.PadRight(25) + processes[i].Album.PadRight(25) + processes[i].Length.PadRight(23);
                    Songs.Items.Insert(index, itm);
                    index++;
                }
            }


            //MessageBox.Show(processes[1].Title);
        }
        
        private void connectIp(object sender, RoutedEventArgs e)
        {
            string test = box.Text;
            Int32 portNum = Int32.Parse(box2.Text);
            Thread tcpclientt = new Thread(() => TcpClient(test, portNum));
            tcpclientt.Start();
            label.Visibility = Visibility.Visible;
            box.Visibility = Visibility.Visible;
            b1.Visibility = Visibility.Visible;
            b2.Visibility = Visibility.Visible;
            lyc.Visibility = Visibility.Hidden;
            b3.Visibility = Visibility.Hidden;
            b4.Visibility = Visibility.Hidden;
            box2.Visibility = Visibility.Hidden;
            box.Clear();
            label2.Visibility = Visibility.Hidden;
            label3.Visibility = Visibility.Hidden;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Songs.Items.Clear();
            label.Visibility = Visibility.Hidden;
            label2.Visibility = Visibility.Hidden;
            label3.Visibility = Visibility.Hidden;
            box.Visibility = Visibility.Hidden;
            b1.Visibility = Visibility.Hidden;
            b2.Visibility = Visibility.Hidden;
            b3.Visibility = Visibility.Hidden;
            b4.Visibility = Visibility.Hidden;
            box2.Visibility = Visibility.Hidden;
            Musicbar.Visibility = Visibility.Hidden;
            lyc.Visibility = Visibility.Visible;
            Songs.Visibility = Visibility.Hidden;
            box.Clear();
        }
        //private void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{




        // }


        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.control.Volume = volumeSlider.Value;
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LoadLyrics();
            lyrics.Visibility = Visibility.Visible;
            back.Visibility = Visibility.Visible;

        }

        private void Back_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            lyrics.Visibility = Visibility.Collapsed;
            back.Visibility = Visibility.Collapsed;

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
        string selectedsong;
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {

            selectedsong = record[Songs.SelectedIndex];
            //MessageBox.Show(selectedsong);
            head.Content = "Edit Song Information";
            Songs.Visibility = Visibility.Collapsed;
            Musicbar.Visibility = Visibility.Collapsed;
            label.Visibility = Visibility.Hidden;
            box.Visibility = Visibility.Hidden;
            b1.Visibility = Visibility.Hidden;
            b2.Visibility = Visibility.Hidden;
            volumeSlider.Visibility = Visibility.Hidden;
            volumeimage.Visibility = Visibility.Hidden;
            panel1.Visibility = Visibility.Hidden;
            panel2.Visibility = Visibility.Hidden;
            label_Copy.Visibility = Visibility.Visible;
            label_Copy1.Visibility = Visibility.Visible;
            label_Copy2.Visibility = Visibility.Visible;
            box_Copy.Visibility = Visibility.Visible;
            box_Copy1.Visibility = Visibility.Visible;
            box_Copy2.Visibility = Visibility.Visible;
            b1_Copy.Visibility = Visibility.Visible;
        }

        private void b1_Copy_Click(object sender, RoutedEventArgs e)
        {
            string tips = "//input new title here";
            head.Content = "          Music";
            volumeSlider.Visibility = Visibility.Visible;
            volumeimage.Visibility = Visibility.Visible;
            panel1.Visibility = Visibility.Visible;
            panel2.Visibility = Visibility.Visible;
            label_Copy.Visibility = Visibility.Hidden;
            label_Copy1.Visibility = Visibility.Hidden;
            label_Copy2.Visibility = Visibility.Hidden;
            box_Copy.Visibility = Visibility.Hidden;
            box_Copy1.Visibility = Visibility.Hidden;
            box_Copy2.Visibility = Visibility.Hidden;
            b1_Copy.Visibility = Visibility.Hidden;
            List<string> lines = new List<string>();
            foreach (ProcessInfo a in processes) {

                if (a.Title == selectedsong && box_Copy.Text != tips)
                    a.Title = box_Copy.Text;
                if (a.Artist == selectedsong && box_Copy1.Text != tips)
                    a.Artist = box_Copy.Text;
                if (a.Album == selectedsong && box_Copy2.Text != tips)
                    a.Artist = box_Copy.Text;
                lines.Add(a.Title + "," + a.Artist + "," + a.Album + "," + a.Length);

            }
            File.WriteAllLines("database/database.txt", lines.ToArray());



        }

        private void lstSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public static void TCPlisten(Int32 portnum)
        {
            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = portnum;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    MessageBox.Show("Connection Successful!Now you can search from this server");
                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    while (true)
                    {
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        }

                        if (data == "close")
                        {
                            // Shutdown and end connection
                            client.Close();
                        }

                        //TODO: check the existence of songs.
                        bool exist = false;

                        // Send result to client
                        if (exist)
                        {
                            byte[] msg = System.Text.Encoding.ASCII.GetBytes("found");
                            // Send back a response.
                            stream.Write(msg, 0, msg.Length);

                            // Send the file
                        }
                        else
                        {
                            byte[] msg = System.Text.Encoding.ASCII.GetBytes("notfound");
                            // Send back a response.
                            stream.Write(msg, 0, msg.Length);
                        }

                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }


            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        public static void TcpClient(string ipaddress, Int32 portNum)
        {
            localAddr = IPAddress.Parse(ipaddress);
            try
            {
                
                Console.WriteLine("Connecting.....");

                tcpclnt.Connect(localAddr, 13000);
                // use the ipaddress as in the server program

                Console.WriteLine("Connected");
                Console.Write("Enter the string to be transmitted : ");

                // String str=Console.ReadLine();
                Stream stm = tcpclnt.GetStream();

                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes("hello from client");
                Console.WriteLine("Transmitting.....");

                stm.Write(ba, 0, ba.Length);

                byte[] bb = new byte[100];
                int k = stm.Read(bb, 0, 100);

                for (int i = 0; i < k; i++)
                    Console.Write(Convert.ToChar(bb[i]));

                tcpclnt.Close();
            }

            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }

        public static void searchP2P(string name)
        {
            // String str=Console.ReadLine();
            Stream stm = tcpclnt.GetStream();
            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] ba = asen.GetBytes(name);
            stm.Write(ba, 0, ba.Length);
        }
    }
}
