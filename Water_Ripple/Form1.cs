using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Water_Ripple
{
    public partial class Form1 : Form
    {
        private Graphics Canvas;
        private static int Rows = 200;
        private static int Columns = 200;
        private double[,] Previous = new double[Rows, Columns];
        private double[,] Current = new double[Rows, Columns];
        private double Dampening = 0.92;
        private static Bitmap bm;
        private object SyncObject = new object();


        public Form1()
        {
            InitializeComponent();

            Canvas = this.CreateGraphics();
            Canvas.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //ThreadPool.QueueUserWorkItem(new WaitCallback(this.Run));
        }

        public async void Run(object o)
        {
            Previous[100, 100] = 0x78FF0000; // drop a colored stone in the middle of the pool

            while (true)
            {
                Draw();
            }
        }

        public async void Draw()
        {
            bm = new Bitmap(Columns, Rows);

            // Conventional loop - way too slow
            //for (int i=1; i < Columns-1; i++)
            //{
            //    for (int j=1; j < Rows-1; j++)
            //    {
            //        Current[i, j] = (   Previous[i - 1, j] +
            //                            Previous[i + 1, j] +
            //                            Previous[i, j - 1] +
            //                            Previous[i, j + 1]) / 2.0 - Current[i, j];
            //        Current[i, j] *= Dampening;
            //        bm.SetPixel(i, j, Color.FromArgb((int)Current[i, j]));
            //    }
            //}

            Parallel.For(1, Columns-1, (i)  =>
            {
                Parallel.For(1, Rows-1, (j) =>
                {
                    Current[i, j] = ( (     Previous[i - 1, j] +
                                            Previous[i + 1, j] +
                                            Previous[i, j - 1] +
                                            Previous[i, j + 1]  ) /  2.0 - Current[i, j] );
                    Current[i, j] *= Dampening;

                    // try to limit values
                    //if (Current[i, j] < -146385000.0)
                    //{
                    //    Current[i, j] = 0;
                    //}

                    // necessary due to concurrent access to bitmap
                    Monitor.Enter(SyncObject);

                    // slow
                    //bm.SetPixel(i, j, Color.FromArgb((int)Current[i, j]));

                    // faster
                    if ((int)Current[i, j] != 0)
                    {
                        bm.SetPixel(i, j, Color.FromArgb((int)Current[i, j]));
                    }

                    Monitor.Exit(SyncObject);
                });
            });

            double[,] Temp = Previous;
            Previous = Current;
            Current = Temp;

            Canvas.DrawImage(bm, new Point(0, 0));
        }

        /*
         * what is missing here is a way to recognize when the "ripple" 'ran out'.
         * The for loop is not optimal, nor is the try to limit iterations, nor is dampening.
         */
        private async void Form1_MouseDown(object sender, MouseEventArgs e)
        {

            Previous[e.X, e.Y] = 0x1EFF1E00; // 0x78FF0000; // drop a colored stone in the middle of the pool

            for ( int r=0; r<100; r++)
            {
                Draw();
            }
            Canvas.Clear(Color.White);
        }
    }
}
