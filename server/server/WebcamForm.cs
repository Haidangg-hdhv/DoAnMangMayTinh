using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.Windows.Forms;

public class WebcamForm : Form
{
    private PictureBox pictureBox;
    private VideoCaptureDevice videoSource;

    public WebcamForm()
    {
        this.Text = "Webcam Preview";
        this.Width = 800;
        this.Height = 600;

        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        this.Controls.Add(pictureBox);

        this.FormClosing += WebcamForm_FormClosing;
        StartWebcam();
    }

    private void StartWebcam()
    {
        var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        if (devices.Count == 0)
        {
            MessageBox.Show("Không tìm thấy webcam!");
            return;
        }

        videoSource = new VideoCaptureDevice(devices[0].MonikerString);

        videoSource.NewFrame += (s, e) =>
        {
            Bitmap frame = (Bitmap)e.Frame.Clone();
            pictureBox.Invoke(new Action(() =>
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = frame;
            }));
        };

        videoSource.Start();
    }

    private void WebcamForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (videoSource != null && videoSource.IsRunning)
        {
            videoSource.SignalToStop();
            videoSource.WaitForStop();
        }
    }
}
