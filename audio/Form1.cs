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
using static audio.Form1;

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
        int levelWork = 97;
        Color selectedColor;
        Thread thread = default;


        public class RGB
        {
            public byte R { set; get; } = 0;
            public byte G { set; get; } = 0;
            public byte B { set; get; } = 0;
        }

        List<Animation> animationsList = new List<Animation>();
        class Animation
        {
            public List<List<RGB>> cadrList = new List<List<RGB>>();
            public decimal delay = 100;
        }

        List<RGB> videoBuff = new List<RGB>();
        List<List<RGB>> cadrList = new List<List<RGB>>();

        bool btn = false;
        public NAudio.Wave.WasapiLoopbackCapture capt;

        //GamePlane global
        public static int point { get; set; } = 4;
        public static bool fire { get; set; } = false;
        public static bool changed { get; set; } = false;
        public void button1_Click(object sender, EventArgs e)
        {
                Console.WriteLine("starting:");
                Console.WriteLine("");

                serialPort1.PortName = textBox1.Text;
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
                    MessageBox.Show("Не удалось подключиться!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                capt = new WasapiLoopbackCapture();
                capt.DataAvailable += OnDataAvailable;
                capt.StartRecording();
                serialPort1.DataReceived += OnDataReceived;

                if(serialPort1.IsOpen) MessageBox.Show("Успешно подключено", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (max > levelWork)
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
                    Thread.Sleep((int)PeriodNumeric.Value);
                    eraseLcd();
                    Thread.Sleep((int)PeriodNumeric.Value);

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
            if(serialPort1.IsOpen) 
                eraseLcd();
            else
                MessageBox.Show("Устройство не подключено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    if (AnimationTextSwitch.Checked)
                    {
                        for (int i = -8; i != 9; i++)
                        {
                            SerialPort1.Write(VideoBufToBuf(pictureRightOffset(el, i)), 0, serialBufLength);
                            Thread.Sleep((int)AnimationTextPeriodNumeric.Value);
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
            if (SlideShowSwitch.Checked)
            {
                thread = new Thread(outPictureOfSavedPicture);
                List<object> obj = new List<object>();

                obj.Add(serialPort1);
                obj.Add((int)PeriodUpdateNumeric.Value);

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
            list.Items.Clear();
            cadrList.ForEach(x => list.Items.Add("Картинка № "+cadrList.IndexOf(x)));
        }

        private void metroToggle2_CheckedChanged(object sender, EventArgs e)
        {
            if(InteractivDrawSwitch.Checked)
            {
                InteractivDrawTimer.Start();
            }
            else
            {
                InteractivDrawTimer.Stop();
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
           btn = AudioActivationSwitch.Checked;
            if (AudioActivationTimer.Enabled) { AudioActivationTimer.Stop(); } else { AudioActivationTimer.Start(); }
        }

        private void metroToggle5_CheckedChanged(object sender, EventArgs e)
        {
            timer2.Enabled = ClockSwitch.Checked;
        }

        private async void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            var numbers = new List<List<RGB>>();
            JsonSerializer serializer = new JsonSerializer();
            List<List<RGB>> list = new List<List<RGB>>();

            using (TextReader textReader = new StreamReader("numbers.txt"))
            {
                numbers.Clear();
                numbers.AddRange((List<List<RGB>>)serializer.Deserialize(textReader, typeof(List<List<RGB>>)));
            }
            counterPicture.Text = numbers.Count().ToString();
            while (ClockSwitch.Checked)
            {
                list.Clear();
                var hour = DateTime.Now.Hour;
                var minute = DateTime.Now.Minute;

                if (hour > 9)
                {
                    list.Add(numbers[Int32.Parse((hour.ToString()[0]).ToString())]);
                    list.Add(numbers[Int32.Parse((hour.ToString()[1]).ToString())]);
                }
                else { list.Add(numbers[hour]); }

                eraseBuf();
                LCD(2, 3, 100, 100, 0);
                LCD(2, 4, 100, 100, 0);
                LCD(3, 3, 100, 100, 0);
                LCD(3, 4, 100, 100, 0);

                LCD(5, 3, 100, 100, 0);
                LCD(5, 4, 100, 100, 0);
                LCD(6, 3, 100, 100, 0);
                LCD(6, 4, 100, 100, 0);

                list.Add(videoBuff);

                if (minute > 9)
                {
                    list.Add(numbers[Int32.Parse((minute.ToString()[0]).ToString())]);
                    list.Add(numbers[Int32.Parse((minute.ToString()[1]).ToString())]);
                }
                else { list.Add(numbers[minute]); }

                foreach (var el in list)
                {
                     for (int i = -8; (i != 9)&&ClockSwitch.Checked; i++)
                     {
                         serialPort1.Write(VideoBufToBuf(pictureRightOffset(el, i)), 0, serialBufLength);
                         await Task.Delay(50);
                     }
                }
                await Task.Delay(100);
            }
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            levelWork = trackBar4.Value;
        }

        private async void metroToggle6_CheckedChanged(object sender, EventArgs e)
        {
            while(AnimationSwitch.Checked)
            {
                foreach(var anim in animationsList)
                {
                    try
                    {
                        for(int i = 0; (i< 5000/(anim.cadrList.Count*((3*anim.delay)+15)))&&AnimationSwitch.Checked; i++)
                        {
                            foreach(var pict in anim.cadrList)
                            {
                                await Task.Delay(2*Convert.ToInt32(anim.delay)+15);
                                serialPort1.Write(VideoBufToBuf(pict), 0, serialBufLength);
                                await Task.Delay(Convert.ToInt32(anim.delay));
                            }
                            var picture = anim;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Не удалось вывести изображение," + Environment.NewLine + "возможно вы не выбрали его.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void showPicBtn_Click(object sender, EventArgs e)
        {
            try
            {
                int index = list.SelectedIndex;
                var picture = cadrList[index];

                serialPort1.Write(VideoBufToBuf(picture), 0, serialBufLength);
            }
            catch
            {
                MessageBox.Show("Не удалось вывести изображение," + Environment.NewLine + "возможно вы не выбрали его.", "Ошибка",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private void removePictBtn_Click(object sender, EventArgs e)
        {
            try
            {
                cadrList.Remove(cadrList[list.SelectedIndex]);

                counterPicture.Text = cadrList.Count().ToString();
                list.Items.Clear();
                cadrList.ForEach(x => list.Items.Add("Картинка № " + cadrList.IndexOf(x)));
            }
            catch
            {
                MessageBox.Show("Не удалось удалить изображение," + Environment.NewLine + "возможно вы не выбрали его.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void metroToggle1_CheckedChanged_1(object sender, EventArgs e)
        {
                if (game1Switch.Checked)
                {
                    thread = new Thread(gamePlane);
                    List<object> obj = new List<object>();

                    obj.Add(serialPort1);
                    thread.Start(obj);
                }
                else
                {
                    thread.Abort();
                    thread.Join(100);
                }
        }

        async void gamePlane(object list)
        {
            var List = (List<object>)list;
            var SerialPort1 = (SerialPort)List[0];
           // var point = GamePlaneClass.point;
            var userDot = new RGB { R = 100, G = 100, B = 0 };

            var userGun = new RGB { R = 255, G = 0, B = 50 };
            int fireCounter = 6;
            int userDotBuf = 0;
            while (true)
            {
                if (changed)
                {
                    eraseLcd();
                    await Task.Delay(7);
                    LCD(7, point, userDot.R, userDot.G, userDot.B);
                    serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
                    Console.WriteLine("ok");
                    await Task.Delay(7);
                    changed = false;
                }
                if(fire)
                {
                    if (fireCounter == 6) userDotBuf = point;
                    LCD(fireCounter, userDotBuf, userGun.R, userGun.G, userGun.B);
                    LCD(7, point, userDot.R, userDot.G, userDot.B);
                    serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
                    await Task.Delay(7);
                    for (int i=0; i<10;i++)
                    {
                        if(changed)
                        {
                            eraseLcd();
                            await Task.Delay(7);
                            LCD(fireCounter, userDotBuf, userGun.R, userGun.G, userGun.B);
                            LCD(7, point, userDot.R, userDot.G, userDot.B);
                            serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
                            changed = false;
                        }
                        await Task.Delay(7);
                    }
                    fireCounter--;
                    if(fireCounter==-1)
                    {
                        fireCounter = 6;
                        fire = false;
                        eraseLcd();
                        await Task.Delay(7);
                        LCD(7, point, userDot.R, userDot.G, userDot.B);
                        serialPort1.Write(VideoBufToBuf(videoBuff), 0, serialBufLength);
                    }
                }
            }

        }

        private void tabControl1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.ToString() == Keys.A.ToString())
            {
                point = point - 1 == -1 ? 7 : point - 1;
                changed = true;
            }
            else
                if (e.KeyChar.ToString() == Keys.D.ToString())
            {
                point = point + 1 == 8 ? 0 : point + 1;
                changed = true;
            }
            if (e.KeyChar.ToString() == Keys.W.ToString())
              fire = true;
             Console.WriteLine("point {0}, fire {1}", point, fire);
             changed = true;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();

            JsonSerializer serializer = new JsonSerializer();

            using (TextReader textReader = new StreamReader(openFileDialog.FileName))
            {
                animationsList.Clear();
                animationsList.AddRange((List<Animation>)serializer.Deserialize(textReader, typeof(List<Animation>)));
            }
            label8.Text = animationsList.Count.ToString();
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.ShowDialog();

            JsonSerializer serializer = new JsonSerializer();

            using (TextWriter textWriter = new StreamWriter(saveFileDialog.FileName))
            {
                serializer.Serialize(textWriter, animationsList);
            }

        }

        private void metroButton3_Click_1(object sender, EventArgs e)
        {
            var animation = new Animation { cadrList = this.cadrList.ToList(), delay = PeriodUpdateNumeric.Value };
            animationsList.Add(animation);
            label8.Text = (Convert.ToInt32(label8.Text) + 1).ToString();
        }

        private void AnimationTextSwitch_CheckedChanged(object sender, EventArgs e)
        {

        }

        private async void metroToggle1_CheckedChanged_2(object sender, EventArgs e)
        {
            while(metroToggle1.Checked)
            {
                SlideShowSwitch.Checked = true;
                await Task.Delay(15000);
                SlideShowSwitch.Checked = false;

                AudioActivationSwitch.Checked = true;
                await Task.Delay(15000);
                AudioActivationSwitch.Checked = false;

                ClockSwitch.Checked = true;
                await Task.Delay(15000);
                ClockSwitch.Checked = false;

                AnimationSwitch.Checked = true;
                await Task.Delay(30000);
                AnimationSwitch.Checked = false;
            }
        }
    }
}
