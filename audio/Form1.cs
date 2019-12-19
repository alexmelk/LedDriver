using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace audio
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        public Form1()
        {
            InitializeComponent();
        }
        SerialPort serialPort1 = new SerialPort();
        Random r = new Random();

        const byte Max_BRIGHTNESS = 020;

        byte R = Max_BRIGHTNESS;
        byte G = 0;
        byte B = Max_BRIGHTNESS;

        const int leds = 64;
        const int ledsInRow = 8;
        const int serialBufLength = leds * 3;

        int brtn = 1000;
        bool flag = false;
        bool flagChanged = false;



        Color selectedColor;
        Thread thread = default;

        public class RGB
        {
            public byte R { set; get; } = 0;
            public byte G { set; get; } = 0;
            public byte B { set; get; } = 0;
        }

        List<RGB> videoBuff = new List<RGB>();
        List<List<RGB>> cadrList = new List<List<RGB>>();

        bool btn = false;
        public NAudio.Wave.WasapiLoopbackCapture capt;
        public void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("starting:");
            Console.WriteLine("");

            serialPort1.PortName = "COM3";
            //serialPort1.PortName = "COM3";
            serialPort1.BaudRate = 115200;
            serialPort1.RtsEnable = true;
            serialPort1.DtrEnable = true;
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Close();
                }
                catch
                {

                }
            }
            try
            {
                serialPort1.Open();
            }
            catch
            {

            }
            capt = new WasapiLoopbackCapture();
            capt.DataAvailable += OnDataAvailable;
            capt.StartRecording();
            serialPort1.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var t = ((SerialPort)sender).ReadLine();
            Console.WriteLine(t);
            flag = true;

        }
        void LCD(int i, int j, byte R, byte G, byte B)
        {
            if ((i > ledsInRow - 1) || (j > ledsInRow - 1) || (i < 0) || (j < 0))
            {
                return;
            }

            if (i == 0)
            {
                videoBuff[j].R = R;
                videoBuff[j].G = G;
                videoBuff[j].B = B;
            }
            if ((i % 2) == 0)
            {
                videoBuff[(ledsInRow) * i + j].R = R;
                videoBuff[(ledsInRow) * i + j].G = G;
                videoBuff[(ledsInRow) * i + j].B = B;
            }
            else
            {
                videoBuff[(ledsInRow) * i + ((ledsInRow - 1) - j)].R = R;
                videoBuff[(ledsInRow) * i + ((ledsInRow - 1) - j)].G = G;
                videoBuff[(ledsInRow) * i + ((ledsInRow - 1) - j)].B = B;
            }
        }

        async void OnDataAvailable(object sender, WaveInEventArgs args)
        {

            float max = 0;
            var buffer = new WaveBuffer(args.Buffer);
            // interpret as 32 bit floating point audio
            for (int index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];

                // absolute value 
                if (sample < 0) sample = -sample;
                // is this the max value?
                if (sample > max) max = sample;

            }
            max *= 100;
            if (max > 97)
            {
                if (btn)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            LCD(i, j, R, G, B);
                        }
                    }
                    serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
                    Thread.Sleep((int)numericUpDown1.Value);
                    eraseLcd();
                    Thread.Sleep((int)numericUpDown1.Value);

                }
            }

        }

        private void trackBar1_ValueChanged(object sender, EventArgs e) //R
        {
            var TR = (TrackBar)sender;
            R = (byte)(TR.Value * 10);

        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)//G
        {
            var TR = (TrackBar)sender;
            G = (byte)(TR.Value * 10);

        }

        private void trackBar3_ValueChanged(object sender, EventArgs e)//B
        {
            var TR = (TrackBar)sender;
            B = (byte)(TR.Value * 10);

        }

        byte[] VideoBufToBuf(List<RGB> videoBuf)
        {
            byte[] buf = new byte[serialBufLength];
            int counter = 0;
            foreach (var el in videoBuf)
            {
                buf[counter] = el.R;
                buf[++counter] = el.G;
                buf[++counter] = el.B;
                counter++;
            }
            return buf;
        }
        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (flagChanged)
            {
                eraseBuf();
                Thread.Sleep(15);
                generation();
                serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
                flagChanged = false;
            }
            await Task.Delay(100);
        }

        private async void metroButton3_Click(object sender, EventArgs e)
        {
            Console.WriteLine(R + G + B);
            for (int i = 0; i < ledsInRow; i++)
            {
                for (int j = 0; j < ledsInRow; j++)
                {
                    LCD(i, j, R, G, B);
                }
            }
            serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
            Console.WriteLine("ok");
            await Task.Delay(100);
            eraseLcd();
            await Task.Delay(100);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < leds; i++)
            {
                videoBuff.Add(new RGB());
            }
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
        }

        void eraseBuf()
        {
            videoBuff = new List<RGB>();
            for (int i = 0; i < leds; i++)
            {
                videoBuff.Add(new RGB());
            }
        }

        void eraseLcd()
        {
            eraseBuf();
            serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
        }
        private void metroButton5_Click(object sender, EventArgs e)
        {
            eraseLcd();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = selectedColor;
            flagChanged = true;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            selectedColor = colorDialog1.Color;
            dataGridView1.RowsDefaultCellStyle.SelectionBackColor = colorDialog1.Color;
        }

        void generation()
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                for (int j = 0; j < dataGridView1.Rows[i].Cells.Count; j++)
                {
                    var color = dataGridView1.Rows[i].Cells[j].Style.BackColor;
                    LCD(i, j, color.R, color.G, color.B);
                }
            }
        }

        private void metroButton8_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                for (int j = 0; j < dataGridView1.Rows[i].Cells.Count; j++)
                {
                    var color = dataGridView1.Rows[i].Cells[j].Style.BackColor;
                    LCD(i, j, color.R, color.G, color.B);
                }
            }

            cadrList.Add(videoBuff);
            counterPicture.Text = cadrList.Count().ToString();
        }


        void outPictureOfSavedPicture(object list)
        {
            var List = (List<object>)list;
            var SerialPort1 = (SerialPort)List[0];
            var Latency = (int)List[1];
            while (true)
            {
                foreach (var el in cadrList)
                {
                    if (metroToggle4.Checked)
                    {
                        for (int i = -8; i != 9; i++)
                        {
                            SerialPort1.Write(VideoBufToBuf(pictureLeftOffset(el, i)), 0, serialBufLength);
                            Thread.Sleep((int)numericUpDown2.Value);
                        }
                    }
                    else
                    {
                        Thread.Sleep(Latency);
                        eraseBuf();
                        Thread.Sleep(15);
                        Thread.Sleep(Latency);
                        SerialPort1.Write(VideoBufToBuf(el), 0, serialBufLength);
                        Thread.Sleep(Latency);
                    }
                    Thread.Sleep(Latency);
                }
            }
        }
        List<RGB> pictureLeftOffset(List<RGB> picture, int offset)
        {
            List<RGB> list = new List<RGB>();
            for (int i = 0; i < leds; i++)
            {
                list.Add(new RGB());
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                int row = (int)(i / ledsInRow);
                int newRow;
                if ((row) % 2 != 0)
                {
                    newRow = (int)((i - offset) / ledsInRow);
                    if (row == newRow)
                    {
                        if (i - offset >= 0)
                        {
                            list[i - offset] = picture[i];
                        }
                    }
                }
                else
                {
                    newRow = (int)((i + offset) / ledsInRow);
                    if (row == newRow)
                    {
                        if (i + offset >= 0)
                        {
                            list[i + offset] = picture[i];
                        }
                    }
                }
            }
            return list;
        }
        List<RGB> pictureRightOffset(List<RGB> picture, int offset)
        {
            return pictureLeftOffset(picture, -offset);
        }
        private void metroToggle1_CheckedChanged(object sender, EventArgs e)
        {
            if (metroToggle1.Checked)
            {
                thread = new Thread(outPictureOfSavedPicture);
                List<object> obj = new List<object>();

                obj.Add(serialPort1);
                obj.Add((int)periodUpdate.Value);

                thread.Start(obj);
            }
            else 
            {
                thread.Abort();
                thread.Join(100);
            }

        }

        private void metroButton6_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.ShowDialog();

            JsonSerializer serializer = new JsonSerializer();

            using (TextWriter textWriter = new StreamWriter(saveFileDialog.FileName))
            {
                serializer.Serialize(textWriter, cadrList);
            }


        }

        private void metroButton9_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();

            JsonSerializer serializer = new JsonSerializer();

            using (TextReader textReader = new StreamReader(openFileDialog.FileName))
            {
                cadrList.Clear();
                cadrList.AddRange((List<List<RGB>>)serializer.Deserialize(textReader, typeof(List<List<RGB>>)));
            }
            counterPicture.Text = cadrList.Count().ToString();
        }

        private void metroToggle2_CheckedChanged(object sender, EventArgs e)
        {
            if(metroToggle2.Checked)
            {
                timer1.Start();
            }
            else
            {
                timer1.Stop();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                for (int j = 0; j < dataGridView1.Rows[i].Cells.Count; j++)
                {
                    dataGridView1.Rows[i].Cells[j].Style.BackColor = dataGridView1.RowsDefaultCellStyle.BackColor;
                }
            }
            eraseLcd();
        }

        private void metroToggle3_CheckedChanged(object sender, EventArgs e)
        {
           btn = metroToggle3.Checked;
        }

        private void metroToggle5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            var numbers = new List<List<RGB>>();
            JsonSerializer serializer = new JsonSerializer();

            using (TextReader textReader = new StreamReader("numbers.txt"))
            {
                numbers.Clear();
                numbers.AddRange((List<List<RGB>>)serializer.Deserialize(textReader, typeof(List<List<RGB>>)));
            }
            counterPicture.Text = numbers.Count().ToString();

            var hour = DateTime.Now.Hour;
            var minute = DateTime.Now.Minute;

            if (hour > 9)
            {
                cadrList.Add(numbers[Int32.Parse((hour.ToString()[0]).ToString())]);
                cadrList.Add(numbers[Int32.Parse((hour.ToString()[1]).ToString())]);
            }
            else { cadrList.Add(numbers[hour]); }


            if (minute > 9)
            {
                cadrList.Add(numbers[Int32.Parse((minute.ToString()[0]).ToString())]);
                cadrList.Add(numbers[Int32.Parse((minute.ToString()[1]).ToString())]);
            }
            else { cadrList.Add(numbers[minute]); }
        }
    }
}
