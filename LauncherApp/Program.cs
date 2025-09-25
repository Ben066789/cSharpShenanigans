using System;
using System.Drawing;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();

        for (int i = 0; i < 10; i++)
        {
            Form form = new Form();
            form.Text = $"Image {i + 1}";
            form.StartPosition = FormStartPosition.Manual;

            // square size
            int size = 200;
            form.ClientSize = new Size(size, size);

            // position (tile next to each other, wrap every 5 images)
            int cols = 5; 
            int row = i / cols;
            int col = i % cols;
            form.Location = new Point(col * (size + 10), row * (size + 40));

            PictureBox pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Image = Image.FromFile(@"C:\Users\COMLAB-PC\Documents\satono.jpg");

            form.Controls.Add(pb);
            form.Show();
        }

        Application.Run();
    }
}
