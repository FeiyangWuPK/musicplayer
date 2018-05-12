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
using System.Windows.Forms;

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
        int count = 0;
        bool ClientResult;
        public IPAddress localAddr;
        public  TcpClient tcpclnt = new TcpClient();
        public TcpClient tcpclnt2 = new TcpClient();
        bool single = false;
        bool cycle = false;
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
            //LoadLyrics();
        }
        void ReadSongs()

        {

            Song song1 = new Song("songs/Demons.mp3");

            this.playlist.AddSong(song1);

            Song song2 = new Song("songs/Radioactive.mp3");

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
            Song song;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MP3 files(*.mp3)| *.mp3 | WAV Files | *.wav | All Files | *.* ";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    if (Path.GetExtension(openFileDialog.FileName) == ".wav")
                    {
                        song = new Song(openFileDialog.FileName);
                        song.Title = Path.GetFileName(openFileDialog.FileName);
                        //song.Artist = "unknown";
                    }
                    else
                    {
                        song = new Song(openFileDialog.FileName);
                    }

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
        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    this.playlist = new Playlist(folderBrowserDialog.SelectedPath);

                    lstSongs.ItemsSource = playlist.Songs;
                    player = new MusicPlayer(playlist);
                    player.SongChanged += new SongChangedEventHandler(player_SongChanged);
                    lstSongs.Items.Refresh();
                    SetBindings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Songs.Items.Clear();
            label2.Visibility = Visibility.Hidden;
            label3.Visibility = Visibility.Hidden;
            //box.Visibility = Visibility.Visible;
            box2.Visibility = Visibility.Hidden;
            b3.Visibility = Visibility.Hidden;
            //b3.Content = "connect";
            b4.Visibility = Visibility.Hidden;
            label.Visibility = Visibility.Visible;
            box.Visibility = Visibility.Visible;
            b1.Visibility = Visibility.Visible;
            b2.Visibility = Visibility.Visible;
            lyc.Visibility = Visibility.Hidden;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            //Songs.Items.Clear();

            label.Visibility = Visibility.Hidden;
            b2.Visibility = Visibility.Hidden;
            b1.Visibility = Visibility.Hidden;
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
            
            StreamReader reader;
            try
            {
                reader = new StreamReader("lyrics/" + player.Playlist.CurrentSong.Title + ".txt");
            }
            catch (IOException e){
                Console.WriteLine("Cannot find lyricsfile",e);
                return;
            }
            while ((line = reader.ReadLine()) != null)
            {

                ListBoxItem itm = new ListBoxItem();
                itm.Content = line;
                lyrics.Items.Insert(index, itm);
                index++;
            }
        }
        List<string> record = new List<string>();

        private void SearchSong_Click(object sender, RoutedEventArgs e)
        {
            string name = box.Text;
            if (!name.Contains("."))
            {
                name = name + ".mp3";
            }
            searchP2P(name);
            record.Clear();
            Songs.Items.Clear();
            Musicbar.Visibility = Visibility.Visible;
            Songs.Visibility = Visibility.Visible;
            int index = 0;
            bool indicate = true;
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
                    indicate = false;
                }



            }
            if (ClientResult&&indicate)
            {
                // MessageBox.Show("aaaaaaaaaa");
                ListBoxItem itm = new ListBoxItem();
                itm.Content = box.Text.PadRight(23) + "This is a Remote Song";
                record.Add(box.Text + " This is a Remote Song");
                Songs.Items.Insert(index, itm);
                index++;
            }


            //MessageBox.Show(processes[1].Title);
        }

        private void connectIp(object sender, RoutedEventArgs e)
        {
            string test = box.Text;
            Int32 portNum = Int32.Parse(box2.Text);
            count++;
            TcpClient(test, portNum);
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
            foreach (ProcessInfo a in processes)
            {

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

        public void TCPlisten(Int32 portnum)
        {
            TcpListener server = null;
            bool exist = false;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = portnum;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                while (true)
                {
                    // TcpListener server = new TcpListener(port);
                    server = new TcpListener(localAddr, port);

                    // Start listening for client requests.
                    server.Start();

                    // Buffer for reading data
                    Byte[] bytes = new Byte[256];
                    String data = null;

                    // Enter the listening loop.

                    //Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    MessageBox.Show("Connection Successful! Now you can search from this server");
                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();


                    int i = 0;

                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);


                        Console.Write("Server side:: ");
                        Console.WriteLine(data);
                        if (data.StartsWith("query"))
                        {
                            data = data.Substring(5);
                            /* for (int j = 0; j < processes.Count; j++)
                             {
                                 if (processes[j].Title == data)
                                 {
                                     exist = true;
                                     break;
                                 }
                             }
                             */
                            exist = System.IO.File.Exists("serversongs/" + data);
                            if (exist == true)
                            {
                                Console.WriteLine("Server:: Found msg sent!");
                                byte[] msg = System.Text.Encoding.ASCII.GetBytes("found");
                                // Send back a response.
                                //Console.WriteLine("Server:: Found msg sent!");
                                Console.WriteLine(msg);
                                stream.Write(msg, 0, msg.Length);



                            }
                            else
                            {
                                byte[] msg = System.Text.Encoding.ASCII.GetBytes("notfound");
                                // Send back a response.
                                Console.WriteLine("Server:: NotFound msg sent!");

                                stream.Write(msg, 0, msg.Length);
                            }

                        }
                        else if (data.StartsWith("download"))
                        {
                            //to do: download
                            data = data.Substring(8);


                            data = "serversongs/" + data;
                            //MessageBox.Show(data);

                            byte[] file = FileToByteArray(data);
                            //MessageBox.Show(file.Length.ToString());
                            stream.Write(file, 0, file.Length);
                            //stream.Write(file, 0, 0);
                            stream.Close();
                            client.Close();
                            break;
                            
                        }
                        else if (data.StartsWith("size"))
                        {
                            data = data.Substring(4);
                            data = "songs/" + data;
                            byte[] file = FileToByteArray(data);
                            byte[] filesize = System.Text.Encoding.ASCII.GetBytes(file.Length.ToString());
                            stream.Write(filesize, 0, filesize.Length);

                        }

                        else if (data.StartsWith("stream"))
                        {

                            data = data.Substring(6);

                            data = "songs/" + data;
                            //MessageBox.Show("stream services!: " + data);
                            byte[] file = FileToByteArray(data);

                            //send the file
                            stream.Write(file, 0, file.Length);


                            stream.Close();
                            client.Close();
                            break;
                        }
                        else if (data.StartsWith("interleave"))
                        {
                            data = data.Substring(10);
                            //int m;
                            int signal = 0;
                            byte[] buf = new byte[10];
                            string number;
                            //number = System.Text.Encoding.ASCII.GetString(buf, 0, buf.Length);
                            //MessageBox.Show(data);
                            signal = Int32.Parse(data);
                            byte[] bmp1 = FileToByteArray("interleave/1-1.bmp");
                            byte[] bmp2 = FileToByteArray("interleave/1-2.bmp");
                            byte[] bmp11 = SubArray(bmp1, 0, bmp1.Length / 4);
                            byte[] bmp12 = SubArray(bmp1, bmp1.Length / 4, bmp1.Length / 4);
                            byte[] bmp13 = SubArray(bmp1, bmp1.Length / 2, bmp1.Length / 4);
                            byte[] bmp14 = SubArray(bmp1, (bmp1.Length / 4) * 3, bmp1.Length - (bmp1.Length / 4) * 3);
                            byte[] bmp21 = SubArray(bmp2, 0, bmp2.Length / 4);
                            byte[] bmp22 = SubArray(bmp2, bmp2.Length / 4, bmp2.Length / 4);
                            byte[] bmp23 = SubArray(bmp2, bmp2.Length / 2, bmp2.Length / 4);
                            byte[] bmp24 = SubArray(bmp2, (bmp2.Length / 4) * 3, bmp2.Length - (bmp2.Length / 4) * 3);
                            
                                //number = System.Text.Encoding.ASCII.GetString(buf, 0, buf.Length);
                                //MessageBox.Show(number);
                                //Console.WriteLine(number);
                                //signal = Int32.Parse(number);
                                if (signal == 1)
                                {
                                    stream.Write(bmp11, 0, bmp11.Length);
                                }
                                else if (signal == 2)
                                {
                                    stream.Write(bmp22, 0, bmp22.Length);
                                }
                                else if (signal == 3)
                                {
                                    stream.Write(bmp13, 0, bmp13.Length);
                                }
                                else if (signal == 4)
                                {
                                    stream.Write(bmp24, 0, bmp24.Length);
                                }
                            }
                            //stream.Close();
                            //client.Close();
                            //server.Stop();
                            //break;


                        

                    }


                    //TODO: check the existence of songs.
                    //bool exist = foundFile(data);



                    // Send result to client

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

        }

        public void TcpClient(string ipaddress, Int32 portNum)
        {
            localAddr = IPAddress.Parse(ipaddress);
            try
            {

                Console.WriteLine("Connecting.....");
                if (count == 1)
                {
                    tcpclnt.Connect(localAddr, portNum);

                }
                else
                {
                    tcpclnt2.Connect(localAddr, portNum);
                }
               
                // use the ipaddress as in the server program
                // String str=Console.ReadLine();
                Stream stm = tcpclnt.GetStream();
                
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes("hello from client");
                Console.WriteLine("Transmitting.....");

                //stm.Write(ba, 0, ba.Length);

            }

            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }


        public void searchP2P(string name)
        {
            try
            {
                Stream stm = tcpclnt.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes("query" + name);
                stm.Write(ba, 0, ba.Length);
                byte[] bb = new byte[100];
                int k;
                string converted = null;
                while ((k = stm.Read(bb, 0, 100)) != 0)
                {

                    converted = System.Text.Encoding.ASCII.GetString(bb, 0, k);
                    //Console.Write(converted);
                    if (converted == "found")
                    {
                        ClientResult = true;
                    }
                    else if (converted == "notfound")
                    {
                        ClientResult = false;
                    }
                    break;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Not connected to any server!" + e.StackTrace);
            }

        }

        public static byte[] FileToByteArray(string fileName)
        {
            return File.ReadAllBytes(fileName);
        }

        /*
         usage: download certain file from server
         invoke: file = downloadP2P(filename);
         */
        public void downloadP2P(string name)
        {
            try
            {
                //get connection stream
                Stream stm = tcpclnt.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes("download"+name);
                stm.Write(ba, 0, ba.Length);
                byte[] bb = new byte[512]; //read 512 bytes of data at a time
                int k=0;
                //create a file to write to
                name = "songs/"+name;
                BinaryWriter writer = new BinaryWriter(File.Open(name, FileMode.Create));
                //stm.Read(bb, 0, bb.Length);
                //writer.Write(bb);
                while ((k = stm.Read(bb, 0, bb.Length)) != 0)
                {
                    
                    writer.Write(bb);
                   
                }
                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Not connected to any server!" + e.StackTrace);
            }

        }
        public static byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public void interleave()
        {
            try
            {
                //get connection stream
                Stream interstream1 = tcpclnt.GetStream();
                Stream interstream2 = tcpclnt2.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes("interleave1");
                byte[] bb = new byte[300000]; //read 128 bytes of data at a time
                int k = 0;
                //create a file to write to
                string name = "interleave/test.bmp";
                BinaryWriter writer = new BinaryWriter(File.Open(name, FileMode.Create));
                
                interstream1.Write(ba, 0, ba.Length);
                if ((k = interstream1.Read(bb, 0, bb.Length)) != 0)
                {
                    Console.WriteLine(k);
                    writer.Write(bb,0,k);
                }
                //interleave signal
                ba = asen.GetBytes("interleave2");
                interstream2.Write(ba, 0, ba.Length);
                
                if ((k = interstream2.Read(bb, 0, bb.Length)) != 0)
                {
                    Console.WriteLine(k);

                    writer.Write(bb,0,k);
                }

                ba = asen.GetBytes("interleave3");
                interstream1.Write(ba, 0, ba.Length);

                if ((k = interstream1.Read(bb, 0, bb.Length)) != 0)
                {
                    Console.WriteLine(k);
                    writer.Write(bb, 0, k);

                }                
                ba = asen.GetBytes("interleave4");
                interstream2.Write(ba, 0, ba.Length);
                
                if ((k = interstream2.Read(bb, 0, bb.Length)) != 0)
                {
                    Console.WriteLine(k);

                    writer.Write(bb,0,k);
                }
                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Not connected to any server!" + e.StackTrace);
            }

        }
        /*
        public void streamP2P(string songname)
        {
            try
            {
                Stream stream = tcpclnt.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                if (!songname.Contains(".wav"))
                {
                    songname += ".mp3";
                }
                byte[] songname_stream = asen.GetBytes("stream" + songname);
                byte[] songsize_stream = asen.GetBytes("size" + songname);
                //send the name of the song
                //stream.Write(songname_stream, 0, songname_stream.Length);
                stream.Write(songsize_stream, 0, songsize_stream.Length);
                
                //receive for the size of the song
                int sizeofsong = 0;
                int k;
                byte[] songsize = new byte[10];
                while ((k=stream.Read(songsize, 0, songsize.Length)) != 0){
                    sizeofsong =Int32.Parse( System.Text.Encoding.ASCII.GetString(songsize, 0, k));
                    
                }
                //request to stream the file
                stream.Write(songname_stream, 0, songname_stream.Length);
                MessageBox.Show("client:: song size: " + sizeofsong.ToString());
                //MessageBox.Show("song size; " + sizeofs);
                //receive and streaming the song file
                int receSize = 0;
                byte[] song_stream = new byte[512];
                BinaryWriter writeSong = new BinaryWriter(File.Open("songs/teststreaming.mp3", FileMode.Create));
                while ((k = stream.Read(song_stream, 0, 512)) != 0)
                {
                    receSize += k;
                    writeSong.Write(song_stream);
                    if (receSize > sizeofsong / 2)
                    {
                        //start playing the song;
                        
                        Song rsong = new Song("songs/"+songname+".mp3");
                        this.playlist.AddSong(rsong);
                        lstSongs.Items.Refresh();
                        player.PlaySong(playlist.Songs.Count - 1);
                        MessageBox.Show("song played?");
                        this.btnPlay.Visibility = Visibility.Collapsed;
                        this.btnPause.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Not connected to any server!" + e.StackTrace);
            }
        }
        */


        private void Songs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("asdasf");
            selectedsong = record[Songs.SelectedIndex];

            if (!selectedsong.Contains("This is a Remote Song"))
            {
               // MessageBox.Show(selectedsong);
                for (int i = 0; i < playlist.Songs.Count; i++)
                {

                    if (playlist.Songs[i].Title == selectedsong)
                    {
                        
                        player.PlaySong(i);
                        this.btnPlay.Visibility = Visibility.Collapsed;

                        this.btnPause.Visibility = Visibility.Visible;
                    }


                }

            }
            

            if (selectedsong.Contains("This is a Remote Song")) {
                //MessageBox.Show("download start");
                string filename = box.Text;
                if (!filename.Contains("."))
                {
                    filename = filename + ".mp3";
                }
                if (count == 1)
                {
                    //MessageBox.Show(filename);
                    Thread downloadt = new Thread(() => downloadP2P(filename));
                    downloadt.Start();
                    downloadt.Join();

                }
                else
                {
                    interleave();
                }
                if (!filename.Contains(".mp4") && count==1)
                {
                    string a = "songs/" + filename;
                    Song rsong = new Song(a);
                   
                    this.playlist.AddSong(rsong);
                    lstSongs.Items.Refresh();
                    //MessageBox.Show(a);
                    player.PlaySong(playlist.Songs.Count-1);
                    this.btnPlay.Visibility = Visibility.Collapsed;
                    this.btnPause.Visibility = Visibility.Visible;
                }
            }

        }

        private void video_Click(object sender, RoutedEventArgs e)
        {
            Search a = new Search();
            a.Show();
        }
        private void single_Click(object sender, RoutedEventArgs e)
        {
            single = true;
            cycle = false;
        }

        private void list_Click(object sender, RoutedEventArgs e)
        {
            cycle = true;
            single = false;

        }

        private void sliderSongProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderSongProgress.Value == this.player.Playlist.CurrentSong.DurationInSeconds - 1)
            {
                if (cycle == true)
                {
                    this.player.PlayNext();
                }
                else if (single == true)
                {
                    this.player.PlayNext();
                    this.player.PlayPrevious();
                }
            }
        }
        
    }
}
