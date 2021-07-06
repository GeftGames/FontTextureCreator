using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

public static class Constants {
        public const float TextBlur =0.7f;

        public const float TextTransparentry=0.25f;
    }
    static class NativeMethods{
        public static Bitmap SetBitmapOpacity(Bitmap image, float opacity, Rectangle rec, int w, int h) {
            Bitmap bmp = new Bitmap(/*image.Width*/w, /*image.Height*/h);

            using (Graphics gfx = Graphics.FromImage(bmp)) {
            gfx.SmoothingMode=SmoothingMode.HighQuality;
            gfx.InterpolationMode=InterpolationMode.HighQualityBicubic;
            gfx.CompositingQuality=CompositingQuality.HighQuality;
            gfx.CompositingMode=CompositingMode.SourceCopy;
            gfx.PixelOffsetMode=PixelOffsetMode.Half;
                ColorMatrix matrix = new ColorMatrix {
                    Matrix33=opacity
                };

                ImageAttributes attributes = new ImageAttributes();

                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            //rec.X+=1;
            //rec.Y+=1;
                gfx.DrawImage(image, /*new Rectangle(0, 0, bmp.Width, bmp.Height)*/rec, 0, 0,image.Width /* w*/, image.Height/*h*/, GraphicsUnit.Pixel, attributes/*attrRed*/);
            }
            return bmp;
        }
  //  static Point p3=new Point(3, 3);

    public static Bitmap BlurX(Bitmap b) {
        Bitmap img = new Bitmap(b.Width, b.Height);
        using (Graphics gg = Graphics.FromImage(img)) {
            gg.PixelOffsetMode=PixelOffsetMode.Half;
            gg.DrawImage(b, new Point(0,0));
        }
        return img;
    }

    public static Bitmap Blur(int due, Bitmap b) {
        Bitmap last=b;
        for (int i=0; i<due; i++){
            Bitmap l=last;
            last=BlurX(last);
            if (i!=due-1) l.Dispose();
            
            
        }
        return last;
    } 

    public unsafe static float CalculateSharpen(Bitmap b){ 
        BitmapData bd=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        float ret=0;
        IntPtr scan=bd.Scan0;
        for (int i=0; i<b.Width*b.Height*4; i+=4){ 
            byte v=(byte)(byte*)(scan+i);
            if (v>5){ 
                int z=v-5;
                ret+=z*z*z;
                //if (v>128) ret+=v*2;//(float)(Math.Tanh(v/255f)*2);
                //else ret-=v;//(float)Math.Tanh(v/255f);
            }
        }

        b.UnlockBits(bd);
        return ret;
    }
        
    class Bm{ 
        public Bitmap bm;
        public float sh;
        public float moveX;
    }

    public static (Bitmap, RectangleF, int rW, int shadowDistance) Text(Font f, string text) {
        int ShadowDistance=2;
        int Bounds=10;
        var pathText = new GraphicsPath();
        
        pathText.AddString(text, f.FontFamily, (int) f.Style, (int)(f.Size*1.45f*2), new Point(1,1), StringFormat.GenericTypographic);
         
        RectangleF recF=pathText.GetBounds();  

        if (recF.Width==0) return (null, recF,0,0);

        float w=recF.Width+/*ShadowDistance+*/Bounds*2, h=recF.Height+/*ShadowDistance+*/Bounds*2;

        if ((int)w%2==1) w++;
        if ((int)h%2==1) h++;

        int iw=(int)w, ih=(int)h;
        int w2=(int)(w/2), h2=(int)(h/2);

        List<Bm> bms=new List<Bm>();
        if (f.Size<20){
            for (float im=0; im</*1*/0.501f; im+=0.05f){ 
                bms.Add(new Bm{
                    bm=new Bitmap(iw, ih),
                    moveX=im,
                });
            }
        }else{ 
            bms.Add(new Bm{
                bm=new Bitmap(iw, ih),
                moveX=0,
            });
        }

        foreach (Bm bb in bms) { 
             using (Graphics g = Graphics.FromImage(bb.bm)) {
                g.SmoothingMode=SmoothingMode.HighQuality;
              //  g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.CompositingMode=CompositingMode.SourceCopy;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;

                Matrix matrix = new Matrix();
                matrix.Translate(-recF.X+bb.moveX+Bounds,-recF.Y+Bounds); //Where offset is the new location
                GraphicsPath dup=(GraphicsPath)pathText.Clone();
                dup.Transform(matrix);
                //g.DrawPath
                g.FillPath(Brushes.Black, dup);
            }
            bb.sh=CalculateSharpen(bb.bm);
        }
       
         Bm[] z = bms.OrderBy(i => i.sh).ToArray();

        Bitmap use=z[0].bm;
        if (z.Length>1){ 
            for (int i=1; i<z.Length; i++){ 
                z[i].bm.Dispose();
            }    
        }

        Bitmap b = new Bitmap(w2/*f+ShadowDistance+Bounds*2*//*+10*/, h2/*f+ShadowDistance+Bounds*2*//*+10*/);
        using( Graphics gh = Graphics.FromImage(b)){
            gh.SmoothingMode=SmoothingMode.HighQuality;
            gh.InterpolationMode=InterpolationMode.HighQualityBicubic;
            gh.PixelOffsetMode=PixelOffsetMode.Half;
            gh.CompositingMode=CompositingMode.SourceCopy;
  
            //Shadow
            {
                Bitmap bbb=SetBitmapOpacity(use,0.05f,new Rectangle(0, 0, w2/*f*/, h2/*f*/), w2/*f*/, h2/*f*/);
                using(Bitmap shadow=new Bitmap((int)(w/2/*f+ShadowDistance+Bounds*2*/)/*+10*/, h2/*f+ShadowDistance+Bounds*2*//*+10*/)){
                    using (Graphics g = Graphics.FromImage(shadow)) {
                        g.PixelOffsetMode=PixelOffsetMode.Half;
                        g.SmoothingMode=SmoothingMode.HighQuality;
                        g.InterpolationMode=InterpolationMode.HighQualityBicubic;

                        for (float x=0.5f; x<4f; x+=0.5f){
                            for (float y=0.5f; y<4f; y+=0.5f){
                                g.DrawImage(bbb, new PointF(x, y));
                            } 
                        }
                    }
                    
                    
                    using( Bitmap shadow2=SetBitmapOpacity(shadow,0.1f, new Rectangle(0, 0, shadow.Width, shadow.Height), shadow.Width, shadow.Height)){
                        gh.DrawImage(shadow2, new Point(0, 0));
                    }
                }

                bbb.Dispose();
                
            }
            gh.PixelOffsetMode=PixelOffsetMode.Half;
            gh.CompositingMode=CompositingMode.SourceOver;
            gh.DrawImage(use, new Rectangle(ShadowDistance, ShadowDistance, w2/*f*/, h2/*f*/), 0, 0, iw, ih,GraphicsUnit.Pixel,attrRed);
            //#if DEBUG
            //gh.SmoothingMode=SmoothingMode.HighSpeed;
            //gh.PixelOffsetMode=PixelOffsetMode.None;
            //gh.DrawRectangle(new Pen(Brushes.Blue), 1, 1, recF.Width/2, recF.Height/2);
            ////gh.DrawRectangle(new Pen(Brushes.Green), 0, 0, b.Width-1, b.Height-1);
            //#endif
        }
            use.Dispose();

        return (/*img*/b, /*Rectangle.Ceiling(*/new RectangleF(recF.X/2f+z[0].moveX,recF.Y/2f+(w/2-w2),recF.Width/2f,recF.Height/2f)/*)*/,Bounds,ShadowDistance);
          //  }

           
            //;
        }


     //   SizeF s=g.MeasureString(text, f); 
     ////   Font ff=new Font(f.FontFamily,f.Size*2);
     //   int w=(int)s.Width+6, h=(int)s.Height+6;
     //    area ;
     //   using (Bitmap img = new Bitmap(w, h)) {
     //       using (Graphics gg = Graphics.FromImage(img)) {
     //         //  using (Bitmap bmbig =new Bitmap(w*2,h*2)) { 
     //         //      using (Graphics g2 = Graphics.FromImage(img)) {
     //         //          g2.CompositingMode=CompositingMode.SourceCopy;
     //         //          g2.CompositingQuality=CompositingQuality.HighQuality;

     //         //          g2.InterpolationMode=InterpolationMode.HighQualityBicubic;
     //         //          g2.SmoothingMode=SmoothingMode.HighQuality;

     //         //          g2.TextContrast=0;
     //         //// gg.PixelOffsetMode=PixelOffsetMode.HighQuality;
     //         //      g2.PixelOffsetMode=PixelOffsetMode.None;
     //         //  g2.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
     //         //  //gg.TextRenderingHint=System.Drawing.Text.TextRenderingHint.SystemDefault;
     //         //  g2.DrawString(text, ff, Brushes.Black, new Point(6, 6));
     //         //  }

     //         //       gg.CompositingMode=CompositingMode.SourceCopy;
     //         //      gg.CompositingQuality=CompositingQuality.HighQuality;

     //         //      gg.InterpolationMode=InterpolationMode.HighQualityBicubic;
     //         //      gg.SmoothingMode=SmoothingMode.HighQuality;

     //         //      gg.DrawImage(bmbig, new Rectangle(0,0,w,h));
     //         //  } 

     //                   gg.CompositingMode=CompositingMode.SourceCopy;
     //             gg.CompositingQuality=CompositingQuality.HighQuality;

     //            gg.InterpolationMode=InterpolationMode.HighQualityBicubic;
     //               gg.SmoothingMode=SmoothingMode.HighQuality;
              
     //        //  gg.DrawPath(,)
     //       }
           

     //       //using (Graphics gg = Graphics.FromImage(img)) {
     //       //    gg.CompositingMode=CompositingMode.SourceCopy;
     //       //    gg.CompositingQuality=CompositingQuality.HighQuality;

     //       //    gg.InterpolationMode=InterpolationMode.HighQualityBicubic;
     //       //    gg.SmoothingMode=SmoothingMode.HighQuality;

     //       //    gg.TextContrast=0;
     //       //  // gg.PixelOffsetMode=PixelOffsetMode.HighQuality;
     //       //  gg.PixelOffsetMode=PixelOffsetMode.None;
     //       //    gg.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
     //       //    //gg.TextRenderingHint=System.Drawing.Text.TextRenderingHint.SystemDefault;
     //       //    gg.DrawString(text, f, Brushes.Black, p3);
     //       //}

     //       using (Bitmap small=new Bitmap((int)(w*Constants.TextBlur), (int)(h*Constants.TextBlur))){ 
     //           int sw=small.Width,sh=small.Height;
     //           Rectangle rec=new Rectangle(0,0,sw,sh);
               

     //           using (Graphics g3 = Graphics.FromImage(small)) {
     //               g3.CompositingMode=CompositingMode.SourceCopy;
     //               g3.CompositingQuality=CompositingQuality.HighQuality;

     //               g3.InterpolationMode=InterpolationMode.HighQualityBicubic;
     //               g3.PixelOffsetMode=PixelOffsetMode.Half; 
     //               g3.SmoothingMode=SmoothingMode.HighQuality;
                    
     //               g3.DrawImage(img,/*new Rectangle(0,0,small.Width,small.Height)*/rec);
     //           }
     //           using (Bitmap tr = SetBitmapOpacity(small, Constants.TextTransparentry, rec, sw, sh)) {
     //               g.CompositingQuality=CompositingQuality.HighQuality;
     //               g.InterpolationMode=InterpolationMode.HighQualityBicubic;
     //               g.CompositingMode=CompositingMode.SourceCopy;
     //               g.SmoothingMode=SmoothingMode.HighQuality;
     //               g.PixelOffsetMode=PixelOffsetMode.Half; 
                    
     //               g.CompositingMode=CompositingMode.SourceCopy;
                    
     //               g.DrawImage(tr, new Rectangle(x/*+1*/, y/*+1*/, w, h));
     //           }

     //      //     g.CompositingQuality=CompositingQuality.HighQuality;
     //        //   g.InterpolationMode=InterpolationMode.HighQualityBicubic;
     //         //  g.SmoothingMode=SmoothingMode.AntiAlias;
     //           g.CompositingMode=CompositingMode.SourceOver;
     //           g.PixelOffsetMode=PixelOffsetMode.Default;
     //                g.DrawImage(img, new Rectangle(x, y, w, h),0,0, w, h,GraphicsUnit.Pixel,attrRed);
     //           }
     //       }
     //   return area;
       // }
  
    static ImageAttributes attrRed;

    public static void ColorImageAttributes() {
        float[][] ptsArray = {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {1, 0, 0, 0, 1}
        };
        
        // Create a ColorMatrix
        ColorMatrix clrMatrix = new ColorMatrix(ptsArray);

        //ColorMap[] colorMap = new ColorMap[1];
        //colorMap[0] = new ColorMap();
        //colorMap[0].NewColor = newColor;
      
        ImageAttributes attr = new ImageAttributes();
        attr.SetColorMatrix(clrMatrix);//.SetRemapTable(colorMap);
        attrRed=attr;
    }

    }
