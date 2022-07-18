using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Diagnostics;
//using class MainForm;

///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    /// 
    public struct RGBPixel
    {
        public byte red, green, blue;
        public RGBPixel(double Red, double Green, double Blue)
        {
            red = (byte)Red;
            green = (byte)Green;
            blue = (byte)Blue;
        }
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {

        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        /// 
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }

        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }

        struct link
        {
            public int parent; // from
            public int child;  // to
            public double value;
        };
        static List<link> list_of_link;

        public static int Make_Quantization(RGBPixel[,] ImageMatrix, int num_K, PictureBox PicBox, bool flag)
        {
            var watch = Stopwatch.StartNew();
            //////////////////////////////////////////////////////////////////////////////////////
            /////// distinct color/////
            List<RGBPixel> distinct_color = new List<RGBPixel>();
            long id1;
            long id_R;
            long id_G;
            long id_B;
            bool[] vaildation = new bool[16777216];
            int Image_width = GetWidth(ImageMatrix);
            int Image_Height = GetHeight(ImageMatrix);

            foreach (RGBPixel pixel in ImageMatrix)     // O(N^2)
            {
                id_R = pixel.red * 65536;
                id_G = pixel.green * 256;
                id_B = pixel.blue;
                id1 = id_R + id_G + id_B;
                if (vaildation[id1] == false)
                {
                    vaildation[id1] = true;
                    distinct_color.Add(pixel);
                }
            }


            /*   for (int i = 0; i < Image_Height; i++)
               {
                   for (int j = 0; j < Image_width; j++)
                   {
                       //Gets a unique ID for each color.
                       //ID = ImageMatrix[i, j].red + ImageMatrix[i, j].green * 256 + ImageMatrix[i, j].blue * 256 * 256; //------> O(1)
                       id1 = (ImageMatrix[i, j].red << 16) + (ImageMatrix[i, j].green << 8) + (ImageMatrix[i, j].blue);
                       if (vaildation[id1] == false) //------> O(1)
                       {
                           distinct_color.Add(ImageMatrix[i, j]); //------> O(1)
                           vaildation[id1] = true; //------> O(1)
                       }
                   }
               }
               */
            MessageBox.Show("Distinct Color = " + distinct_color.Count.ToString());
            //////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////MST///////////////////////////////////

            double[] weigth = new double[distinct_color.Count];
            bool[] viste = new bool[distinct_color.Count];
            int[] parnt = new int[distinct_color.Count];
            int[] ch = new int[distinct_color.Count];

            for (int i = 0; i < distinct_color.Count; i++)
            {
                weigth[i] = double.MaxValue;
                viste[i] = false;
            }

            weigth[0] = 0;
            double sum = 0;
            int num_Node = 0;
            // Construct graph o(D^2)
            while (num_Node < distinct_color.Count)
            {
                int index_of_min = -1;
                double min = double.MaxValue;
                for (int j = 0; j < distinct_color.Count; j++)
                {
                    if (viste[j] == false && weigth[j] < min)
                    {
                        min = weigth[j];
                        index_of_min = j;
                    }
                }
                viste[index_of_min] = true;

                for (int x = 0; x < distinct_color.Count; x++)
                {
                    double dis = distance(distinct_color, index_of_min, x);
                    if (dis < weigth[x] && viste[x] == false && dis > 0)
                    {
                        weigth[x] = dis;

                        parnt[x] = index_of_min;
                        ch[x] = x;
                    }
                }
                num_Node++;
            }
            list_of_link = new List<link>();
            for (int i = 0; i < distinct_color.Count; i++)  // mst = O(D)
            {
                sum += weigth[i];
                list_of_link.Add(new link()
                {
                    parent = parnt[i],
                    child = ch[i],
                    value = weigth[i]
                });

            }
            MessageBox.Show("MST = " + sum.ToString());

            {
                /*
                for (int i = 0; i < list_of_link.Count; i++)
                {
                    Console.WriteLine("index = " + i);
                    Console.WriteLine("parent = " + list_of_link[i].parent);

                    Console.WriteLine("child = " + list_of_link[i].child);
                    Console.WriteLine("value >> " + list_of_link[i].value);

                    Console.WriteLine("********************");

                }
                Console.WriteLine("__________________________________________");*/

            }
            ////////////////////////////////////////////////////////////////////////////////////
            ///////////////////// CLUSTER //////////////////////////////////////////////////
            ///
            if (flag == true)
            {
                num_K = Detect_K_number(list_of_link);
                Console.WriteLine("*****************************");
                Console.WriteLine("*****************************");

                Console.WriteLine("K= " + num_K);
            }
            if (num_K == -1)
            {
                num_K = list_of_link.Count;
            }

            for (int k = 0; k < num_K - 1; k++)
            {
                int index = 0;
                double max_weight = 0;
                for (int h = 0; h < list_of_link.Count; h++)
                {

                    if (list_of_link[h].value > max_weight) //------> O(1)
                    {
                        index = h;                          //------> O(1)
                        max_weight = list_of_link[h].value; //------> O(1)
                    }
                }
                int o_child = list_of_link[index].child;
                list_of_link[index] = new link()
                {
                    parent = index,
                    child = o_child,
                    value = -1
                };
            }

            HashSet<int>[] adj = new HashSet<int>[list_of_link.Count];
            HashSet<int>[] list_of_clusters = new HashSet<int>[num_K];
            HashSet<RGBPixel> list_palette = new HashSet<RGBPixel>();

            for (int i = 0; i < num_K; i++)
            {
                list_of_clusters[i] = new HashSet<int>();
            }
            for (int s = 0; s < list_of_link.Count; s++)
            {
                adj[s] = new HashSet<int>();
            }
            for (int w = 0; w < list_of_link.Count; w++)
            {
                if (list_of_link[w].parent != w)
                {
                    int x = list_of_link[w].parent;
                    // Console.WriteLine("x =" + x);
                    //HashSet<int> temp = new HashSet<int>();
                    //temp.Add(x);
                    adj[w].Add(x);
                    //temp.Remove(x);
                    //temp.Add(w);
                    adj[list_of_link[w].parent].Add(w);
                    //Console.WriteLine("W= "+w);
                }
            }
            bool[] v = new bool[adj.Length];
            for (int i = 0; i < adj.Length; i++)                           // o(D)
            {
                v[i] = false;
            }


            int num_of_empty_lists = 0;
            int temp_new_adj = 0;
            for (int i = 0; i < adj.Length; i++)
            {
                if (adj[i].Count == 0 && v[i] == false)
                {
                    v[i] = true;
                    num_of_empty_lists++;
                    list_of_clusters[temp_new_adj].Add(i);
                    temp_new_adj++;
                }
            }
            int remain_num_cluster = num_K - num_of_empty_lists;

            int cont = 0;
            for (int y = 0; y < remain_num_cluster; y++)
            {
                {
                    // List<int>[]l;
                    // for (int i = 0; i < adj.Length; i++) {

                    /* if (v[i] == false) {
                          //  l[i] = new List<int>();
                          // HashSet<int>[] test = new HashSet<int>();
                          //list_of_clusters[i].Add(dfs(i));

                          dfs(temp_new_adj , i);
                          temp_new_adj++;
                      }*/

                    //{
                }

                if (cont < adj.Length)
                {
                    while (adj[cont].Count == 0 || v[cont] == true)
                    {
                        cont++;
                    }

                    if (v[cont] == false && adj[cont].Count != 0)
                    {
                        list_of_clusters[y + temp_new_adj].Add(cont);
                        v[cont] = true;
                        foreach (var x in adj[cont])
                        {
                            if (v[x] == false)
                            {
                                int index_x = x;
                                dfs(y + temp_new_adj, index_x);
                            }
                        }
                    }
                }
                cont++;
            }

            void dfs(int i, int u)
            {
                {
                    /* list_of_clusters[i].Add(u);
                     v[u] = true;

                     foreach (var x in adj[u])
                     {
                         if (v[x] == false && adj[x].Count != 0)
                         {
                             int index_x = x;
                             dfs(i ,index_x);
                         }

                     }*/


                }

                v[u] = true;                // O(1)
                list_of_clusters[i].Add(u);  //O(1)

                foreach (var x in adj[u])   //O(D)
                {
                    if (v[x] == false && adj[x].Count != 0) // O(1)
                    {
                        dfs(i, x);
                    }
                }
            }


            {
                /*
                for (int k = 0; k < list_of_clusters.Length; k++)

                {

                    Console.WriteLine("[" + k +"]");
                        for (int j = 0; j < list_of_clusters[k].Count; j++)
                        {
                         Console.Write(" " + list_of_clusters[k][j]);
                        Console.WriteLine( " red = " +distinct_color[list_of_clusters[k][j]].red);
                        Console.WriteLine(" g = " + distinct_color[list_of_clusters[k][j]].green);
                        Console.WriteLine(" b = " + distinct_color[list_of_clusters[k][j]].blue);

                        // Console.WriteLine("weight= " + weights[k]);
                    }

                    Console.WriteLine();
                    Console.WriteLine("**************");
                }*/

            }

            ////////////////////////////////////////////////////////////////////////////////////
            ////////////////////AVERAGE///////////////////////////////////////////////////
            double sum_of_red;     //O(1)
            double sum_of_green;     //O(1)
            double sum_of_blue;     //O(1)
            RGBPixel[] Guide = new RGBPixel[16777217];      //O(1)
            int result;   //O(1)
            int r;   //O(1)
            int g;   //O(1)
            int b;   //O(1)

            for (int i = 0; i < list_of_clusters.Length; i++)   // O(D) = O(KSub(d))
            {
                sum_of_red = 0;
                sum_of_green = 0;
                sum_of_blue = 0;

                foreach (var x in list_of_clusters[i])
                {
                    sum_of_red += distinct_color[x].red;
                    sum_of_blue += distinct_color[x].blue;
                    sum_of_green += distinct_color[x].green;
                }

                sum_of_red = sum_of_red / list_of_clusters[i].Count;
                sum_of_blue = sum_of_blue / list_of_clusters[i].Count;
                sum_of_green = sum_of_green / list_of_clusters[i].Count;

                RGBPixel obj = new RGBPixel(sum_of_red, sum_of_green, sum_of_blue);
                list_palette.Add(obj);
                foreach (var x in list_of_clusters[i])
                {
                    r = distinct_color[x].red * 65536;
                    g = distinct_color[x].green * 256;
                    b = distinct_color[x].blue;
                    result = r + g + b;
                    Guide[result] = obj;
                }

            }

            for (int h = 0; h < GetHeight(ImageMatrix); h++)
            {
                for (int w = 0; w < GetWidth(ImageMatrix); w++)
                {
                    result = ImageMatrix[h, w].red * 65536 + ImageMatrix[h, w].green * 256 + ImageMatrix[h, w].blue;
                    ImageMatrix[h, w] = Guide[result];
                }
            }

            DisplayImage(ImageMatrix, PicBox);

            //cluster_using_set_union
            {
                /* for (int i = 0; i < adj.Length; i++) {
                   if (adj[i].Count != 0) {
                      // v[i] = true;
                       foreach (var x in adj[i]){
                           if (v[x] == false) {
                               int index_of_x = x;
                               dfs(i, index_of_x);
                           } 
                       }

                   }
               }
                void dfs(int z , int u) {
                 //  v[u] = true;
                  // foreach (var x in adj[u]){
                       if (v[u] == false && adj[u].Count !=0) {
                           adj[z].UnionWith(adj[u]);
                             v[u] = true;
                       foreach (var f in adj[z]) {
                           Console.WriteLine($"{f} ,");
                       }
                       //}
                   }

                   v[z] = true;
               }

            */

            }

            watch.Stop();
            MessageBox.Show("minute time = " + (watch.ElapsedMilliseconds) / 60000 + " m " + "\n" +
                " second time = " + (watch.ElapsedMilliseconds) / 1000 + " s " + "\n" +
                " milli seconds time = " + watch.ElapsedMilliseconds + " ms ");


            // print adj
            {
                /*

                //////////////////// print hash set ///////////////////

                int count = 0;
                //adj[1].UnionWith(adj[2]);
                for (int i = 0; i < adj.Length; i++)
                {
                    //if (adj[count].Count != 0)
                    // {

                   /* if (adj[i].Count == 0)
                    {
                        Console.WriteLine("[" + i + "]");
                    }
                    foreach (var d in adj[count])
                        {


                            Console.WriteLine( "["+i+"]"+$"{d} ,");
                        }

                        Console.WriteLine("---------------------------");
                  //  }
                    count++;
                }
                Console.WriteLine("())))))))))))))))))))))))))))))))))))))))))))");
                Console.WriteLine(adj.Length);
                        //count++;
                   // }

                */

            }

            {
                /*
                            for (int k = 0; k < adj.Length; k++)

                            {
                               for (int j = 0; j < adj[k].Count; j++)
                                 {
                                     Console.Write(" " + adj[k][j]);
                                     // Console.WriteLine("weight= " + weights[k]);
                                 }

                                Console.WriteLine();
                                Console.WriteLine("**************");
                            }
                            */
                // HashSet<int>[] adjset = new HashSet<int>[adj.Length];

                /*  for (int s = 0; s < adj.Length; s++)
                  {
                      //adj[s] = new List<int>(list_of_link.Count);
                      adjset[s] = new HashSet<int>();

                     // Console.Write(" " + adj[][]);
                  }
                  */
            }

            {
                /*
               Console.WriteLine("&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&");
               for (int k = 0; k < adjset.Length; k++)
               {
                   for (int j = 0; j < adjset[k].Count; j++)
                   {
                       Console.Write(" " + adj[k][j]);
                       // Console.WriteLine("weight= " + weights[k]);
                   }
                   Console.WriteLine();
                   Console.WriteLine("**************");
               }

               */
            }

            {
                /* HashSet<int> xx= new HashSet<int>();
            HashSet<int> y = new HashSet<int>();
            xx.Add(1);
            xx.Add(2);
            xx.Add(3);

           foreach (var c in xx) {
               Console.WriteLine($"{c} ,");
           }*/

                //y.Add(4);
                //y.Add(5);
                //y.Add(6);

                // for (int i = 0; i < xx.Count; i++) {

                // xx.UnionWith(y);

                //Console.WriteLine(xx.Contains(6));


                //}
            }

         
            distinct_color.Clear();
            list_of_link.Clear();
            list_palette.Clear();
            return num_K;
        }

        private static int Detect_K_number(List<link> List_OF_Link)
        {
            double mean = 0, sum = 0, new_SV = double.MaxValue, old_SV = 0, max_sv = 0;
            int Num_K = 0, index_sv = -1, count;

            List<link> data = new List<link>(List_OF_Link);
            Console.WriteLine("*****************************");
            while (Math.Abs(new_SV - old_SV) > 0.0001)
            {
                Console.WriteLine("EQ=  "+ Math.Abs(new_SV - old_SV));
                Console.WriteLine("counter=  " +data.Count);
                Console.WriteLine("++++++++");
                if (data.Count > 0)
                {
                    old_SV = new_SV;
                    foreach (link lk in data)
                    {
                        mean += lk.value;
                    }
                    mean = mean / data.Count;
                    count = 0;
                    index_sv = -1;
                    max_sv = 0;
                  foreach (link lk in data)
                    {
                        double term = (lk.value - mean);
                        term = term * term;
                        if (term > max_sv)
                        {
                            max_sv = term;
                            index_sv = count;
                        }
                        count++;
                        sum += term;
                    }

                    new_SV = sum / (data.Count-1);
                    new_SV = Math.Sqrt(new_SV);
                    bool res = Double.IsNaN(new_SV);
                    if (res == true || new_SV == 0)
                    {
                        index_sv = 0;
                    }
                   data.RemoveAt(index_sv);
                    
                    mean = 0;
                    sum = 0;
                    Num_K++;
                }
                
            }
            Num_K--;
            return Num_K;
        }

        static double distance(List<RGBPixel> dc, int parent_index, int child_index)
        {

            double r = (dc[parent_index].red - dc[child_index].red);
            r = r * r;

            double g = (dc[parent_index].green - dc[child_index].green);
            g = g * g;

            double b = (dc[parent_index].blue - dc[child_index].blue);
            b = b * b;

            return Math.Sqrt(r + g + b);
        }

    }

    

}
