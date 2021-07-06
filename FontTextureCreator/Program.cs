using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml;

namespace FontTextureCreator {
    static class Program {
        public static void Main() {
            List<int> fonts=new List<int>{ 34, 18 };
            foreach (int i in fonts) { 
                using (ProgramJob pj=new ProgramJob()) pj.Main(i);
            }
        }
    }
 
    class ProgramJob : IDisposable {
        readonly string[] donotsave={"´", "¨", "˛", "`", "ˇ", "˘", "⺅", "⺉", "⼥", "ﾟ", "゛", "ﾞ", "◯", "¯", "˝"};

        readonly int[] bigFontSaveTextIds={
            6, /*Singleplayer*/
            7,// /*Multiplayer*/
            8, /*Settings*/
            121,    /*Language*/
            5, /*Information*/
            9, //   /*Styles*/
            10,  //  /*Close*/
            82,//Character
            182, //Rabigon...
        };

        /*
            Make sure that You dont foget to do this after modified game
            MonoGame mgcb -> Rebuild! (output Fonts copy to Publish folder for test)
            Check that all bin folders are in the project
            Game load bin files in Rabcr.SetLangUp method, so if new or remove ...
        */

        const string 
            // True Type font file, make sure that all are installed
            FontPathLatin_Arabic_Cyrillic=@"C:\Users\GeftGames\source\repos\GeftGames\FontTextureCreator\FontTextureCreator\Fonts\latin+arabiv+cyrilic\GGF_latin_arabic_cyrilic.ttf",
            FontPathDevanagari=@"C:\Users\GeftGames\source\repos\GeftGames\FontTextureCreator\FontTextureCreator\Fonts\devanagari\GGF_Devanagari.ttf",
            FontPathTraditionalChinese=@"C:\Users\GeftGames\source\repos\GeftGames\FontTextureCreator\FontTextureCreator\Fonts\traditional chinese\GGF_traditionalChinese.ttf",
            FontPathKorean=@"C:\Users\GeftGames\source\repos\GeftGames\FontTextureCreator\FontTextureCreator\Fonts\korean\GGF_korean.ttf",
            FontPathJapanese=@"C:\Users\GeftGames\source\repos\GeftGames\FontTextureCreator\FontTextureCreator\Fonts\japanese\GGF_japanese.ttf",
            allkanjitorange=@"C:\Users\GeftGames\source\repos\GeftGames\FontTextureCreator\FontTextureCreator\allkanjitorange.txt",
            // Output Texture
            BitmapTexturePath=@"C:\Users\GeftGames\source\repos\GeftGames\rabcrClient\rabcrClient\Default\Fonts",
            
            // Output binnary font data - positions and sizes
            FontInfo=@"C:\Users\GeftGames\source\repos\GeftGames\rabcrClient\rabcrClient\Resources",
            
            // Lang file
            LangFileXML=@"C:\Users\GeftGames\source\repos\GeftGames\rabcrClient\rabcrClient\Default\Lang\Lang.xml";

        const string charsEverytime="AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz !\"#$%&'()*+,-./0123456789:;<=>?@[\\]_{|}~©·÷×€\u2639\u263a\u2764";//\u7228

        List<LangFile> LangFiles;

        public unsafe void Main(int ss) {
            int fontsize=ss;
            Console.WriteLine("Font size: "+ss);
            Console.CursorVisible=false;
            LangFiles=new List<LangFile>();

            {
                XmlDocument file = new XmlDocument();
                try {
                    file.Load(LangFileXML);  
                } catch(Exception ex){ 
                    MessageBox.Show("Language file is corrupted/Jazykový soubor je poškozen\r\nCheck file \"Lang.xml\"\r\n\r\nDetails/Podrobnosti:\r\n"+ex.Message,"Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

                if (fontsize==18){ 
                    foreach(XmlNode node in file.DocumentElement.ChildNodes) {
                        if (node.Name=="Langs") { 
                            foreach (XmlNode n in node.ChildNodes) {
                                if (n.Attributes["EnglishName"].Value==null) continue;
                                if (n.Attributes["NativeName"].Value==null) continue;

                                if (n.Attributes["FontFile"]!=null) { 
                                    string name=n.Attributes["FontFile"].Value; 

                                    bool exists=false;
                                    foreach (LangFile ff in LangFiles){ 
                                        if (ff.name==name){
                                            exists=true;
                                            break;    
                                        }
                                    }

                                    if (!exists) { 
                                        LangFile lf=new LangFile { 
                                           name=name,
                                           BuildingRange=new List<FromTo>(),
                                           chars=new List<SavedChar>(),
                                           quality=1,
                                          // fontFile=FontPath
                                        };

                                        SolveChars(lf,charsEverytime);
                                        if (lf.name=="japanese") {
                                           // lf.BuildingRange.Add(new FromTo{ From=8216, To=15017 }); 
                                          //  lf.BuildingRange.Add(new FromTo{ From=17366, To=65533 }); 
                                          lf.BuildingRange.AddRange(JapaneseRange());
                                            lf.fontFile=FontPathJapanese;

                                        }  else if (lf.name=="korean") {
                                            lf.BuildingRange.Add(new FromTo{ From=0, To=500000 }); 
                                           // lf.BuildingRange.Add(new FromTo{ From=15017, To=17366 }); 
                                            lf.fontFile=FontPathKorean;

                                        }else if (lf.name=="devanagari") {
                                            lf.BuildingRange.Add(new FromTo{ From=2309, To=2401 }); 
                                            lf.BuildingRange.Add(new FromTo{ From=2325, To=2399 }); 
                                            lf.BuildingRange.Add(new FromTo{ From=2305, To=2405 }); 
                                            lf.BuildingRange.Add(new FromTo{ From=2406, To=2416 }); 
                                           // lf.BuildingRange.Add(new FromTo{ From=15017, To=17366 }); 
                                            lf.fontFile=FontPathDevanagari;

                                        }else if (lf.name=="traditionalChinese") { 
                                            lf.BuildingRange.Add(new FromTo{ From=12289, To=65533 });
                                            lf.fontFile=FontPathTraditionalChinese;
                                           
                                        } else if (lf.name=="arabic") {
                                            lf.BuildingRange.Add(new FromTo{ From=1536, To=1791 }); 
                                            lf.BuildingRange.Add(new FromTo{ From=1872, To=1919 });
                                            lf.BuildingRange.Add(new FromTo{ From=2208, To=2303 });
                                            lf.BuildingRange.Add(new FromTo{ From=64336, To=65023 });
                                            lf.BuildingRange.Add(new FromTo{ From=65136, To=65279 });
                                            lf.BuildingRange.Add(new FromTo{ From=69216, To=69247 });
                                            lf.fontFile=FontPathLatin_Arabic_Cyrillic;

                                        } else  if (lf.name=="latin") {
                                              lf.fontFile=FontPathLatin_Arabic_Cyrillic;
                                        } else  if (lf.name=="cyrillic") {
                                              lf.fontFile=FontPathLatin_Arabic_Cyrillic;
                                        }
                                        LangFiles.Add(lf);
                                    }
                                }
                            }
                            break;
                        }
                    }
                     
                    foreach(XmlNode node in file.DocumentElement.ChildNodes) {
                    if (node.Name=="Langs") { 
                        foreach (XmlNode n in node.ChildNodes) {
                            if (n.Attributes["EnglishName"].Value==null) continue;
                            if (n.Attributes["NativeName"].Value==null) continue;

                            if (n.Attributes["FontFile"]!=null) { 
                                string name=n.Attributes["FontFile"].Value; 

                                foreach (LangFile ff in LangFiles){ 
                                    if (ff.name==name){
                                        if (fontsize==18) {
                                            SolveChars(ff,n.Attributes["NativeName"].Value);
                                            SolveChars(ff,n.Attributes["EnglishName"].Value);
                                        }
                                        if (n.Attributes["alphabet"]!=null) SolveChars(ff,n.Attributes["alphabet"].Value);

                                    }
                                }
                            }

                            foreach (LangFile ff in LangFiles){
                                if (fontsize==18) {
                                    SolveChars(ff, n.Attributes["NativeName"].Value);
                                    SolveChars(ff, n.Attributes["EnglishName"].Value);
                                }
                            }
                        }
                        break;
                    }
                }

                } else {
                    foreach(XmlNode node in file.DocumentElement.ChildNodes) {
                        if (node.Name=="Langs") { 
                            foreach (XmlNode n in node.ChildNodes) {
                                if (n.Attributes["EnglishName"].Value==null) continue;
                                if (n.Attributes["NativeName"].Value==null) continue;

                                if (n.Attributes["FontFile"]!=null) { 
                                    string name=n.Attributes["FontFile"].Value; 

                                    bool exists=false;
                                    foreach (LangFile ff in LangFiles){ 
                                        if (ff.name==name){

                                            string langnodename=n.Attributes["Name"].Value;

                                            ff.languages.Add(langnodename);
                                            exists=true;
                                            break;    
                                        }
                                    }

                                    if (!exists) { 
                                        LangFile lf=new LangFile { 
                                           name=name,
                                           BuildingRange=new List<FromTo>(),
                                           chars=new List<SavedChar>(),
                                           quality=1,
                                           languages=new List<string>{n.Attributes["Name"].Value }
                                        };

                                        if (lf.name=="traditionalChinese") { 
                                             lf.fontFile=FontPathTraditionalChinese;
                                             
                                        } if (lf.name=="korean") { 
                                             lf.fontFile=FontPathKorean;
                                             
                                        } if (lf.name=="japanese") { 
                                             lf.fontFile=FontPathJapanese;

                                        } else if (lf.name=="arabic") {
                                            lf.BuildingRange.Add(new FromTo{ From=1536, To=1791 }); 
                                            lf.BuildingRange.Add(new FromTo{ From=1872, To=1919 });
                                            lf.BuildingRange.Add(new FromTo{ From=2208, To=2303 });
                                            lf.BuildingRange.Add(new FromTo{ From=64336, To=65023 });
                                            lf.BuildingRange.Add(new FromTo{ From=65136, To=65279 });
                                            lf.BuildingRange.Add(new FromTo{ From=69216, To=69247 });
                                            lf.BuildingRange.Add(new FromTo{ From=64472, To=64472});
                                            lf.BuildingRange.Add(new FromTo{ From=64476, To=64476});
                                            lf.BuildingRange.Add(new FromTo{ From=64429, To=64429});
                                            lf.fontFile=FontPathLatin_Arabic_Cyrillic;
                                        } else { 
                                            lf.fontFile=FontPathLatin_Arabic_Cyrillic;
                                        }

                                        LangFiles.Add(lf);
                                    }
                                }
                            }
                            break;
                        }
                    }

                    XmlNode data=null;

                    foreach (XmlNode node in file.DocumentElement.ChildNodes) {
                        if (node.Name=="Data") { 
                            data=node;
                            break;    
                        }
                    }

                    // <Data>
                    foreach (XmlNode node in data.ChildNodes) {

                        // <String id='?'>
                        if (int.TryParse(node.Attributes[0].Value, out int v)) {
                           // bool isForBig=false;
                            foreach (int b in bigFontSaveTextIds){ 
                                if (b==v)  { 

                                    // <String id='45'>
                                    foreach (XmlNode n in node.ChildNodes) {
                                        //<xxx>blabla</xxx>

                                        // xxx is lang with some script
                                        LangFile l=GetScriptFromNameNode(n.Name);
                                        SolveChars(l,n.InnerText);
                                   

                                        LangFile GetScriptFromNameNode(string namenode) { 
                                            foreach (LangFile lf in LangFiles){ 
                                                foreach (string nn in lf.languages){ 
                                                    if (nn==namenode)return lf;    
                                                }
                                            }
                                            throw new Exception ("Unknown language "+namenode+", or missing FontFile atribute in <String id='"+v+"'>");
                                        }
                                    } 
                                }
                            }
                        } else { throw new Exception("Lang.xml file has node String with wrong atribute id"); }
                    }
                }

                foreach (LangFile lf in LangFiles) SolveChars(lf,"�");
              
                void SolveChars(LangFile ff, string addchars){
                    if (ff.name=="arabic"){ 
                        addchars=addchars.Replace(((char)1617).ToString()+((char)1614).ToString(),"\uFC62");
                        addchars=addchars.Replace(((char)1575).ToString()+((char)1604).ToString(),"\uFEFB");
                        addchars=addchars.Replace(((char)1573).ToString()+((char)1604).ToString(),"\uFEF9");
                        
                        char[] chs=addchars.ToCharArray();
                        
                        for (int ch = 0; ch<chs.Length; ch++) {
                            int numChar=chs[ch];
                            bool spaceBef, spaceAf;

                            if (ch==0)spaceBef=true;
                            else {
                                if (IsAsrabicDisconecter(chs[ch-1])) {
                                    if (ch==1) spaceBef=true;
                                    else spaceBef=IsSelfishR(chs[ch-2]);
                                } else {
                                    spaceBef=IsSelfishR(chs[ch-1]);
                                }
                            }

                            if (ch==chs.Length-1)spaceAf=true;
                            else {
                                if (IsAsrabicDisconecter(chs[ch+1])) {
                                    if (ch==chs.Length-2) spaceAf =true;
                                    else spaceAf=IsSelfishL(chs[ch+2]);
                                } else {
                                    spaceAf =IsSelfishL(chs[ch+1]);
                                }
                            }
                                                        
                            if (spaceBef) {
                                if (spaceAf) {

                                } else {
                                    numChar=ArabicFinalFormConventer(numChar);
                                }
                            } else {
                                if (spaceAf){
                                   numChar=ArabicInitialFormConventer(numChar);
                                } else{
                                   numChar=ArabicMedialFormConventer(numChar);
                                }
                            }

                            AddChar(numChar); 
                        } 
                    } else { 
                        foreach (char ch in addchars) AddChar(ch); 
                    }

                    void AddChar(int c) { 
                        // Zkontroluj zda již znak existuje v seznamu
                        foreach (FromTo x in ff.BuildingRange) {
                            if (x.From<=c) {
                                if (x.To>=c) {
                                    return;
                                }
                            }
                        }
                                            
                        ff.BuildingRange.Add(new FromTo {
                            From=c,
                            To=c,
                        });
                    }
                }
            }

            foreach (LangFile lxf in LangFiles){
                lxf.Range=lxf.BuildingRange.ToArray();
                if (lxf.name=="japanese"){
                    if (fontsize==18) lxf.quality=255/8;
                    else lxf.quality=255/5;
                    lxf.fontFile=FontPathJapanese;
                }else if (lxf.name=="korean"){
                    if (fontsize==18) lxf.quality=255/8;
                    else lxf.quality=255/5;
                    lxf.fontFile=FontPathKorean;
                }else if (lxf.name=="devanagari"){
                    if (fontsize==18) lxf.quality=255/8;
                    else lxf.quality=255/5;
                    lxf.fontFile=FontPathDevanagari;
                }else if (lxf.name=="traditionalChinese"){
                    if (fontsize==18) lxf.quality=255/8;
                    else lxf.quality=255/5;
                    lxf.fontFile=FontPathTraditionalChinese;
                }else if (lxf.name=="latin"){ 
                    if (fontsize==18) lxf.quality=1;
                    else lxf.quality=/*16*/1;
                }else{ 
                    if (fontsize==18) lxf.quality=/*4*/1;
                    else lxf.quality=/*16*/1;
                }
            }
         
            Console.WriteLine("Check support of glyphs in font file");
            Support();
            
            #region Check support for all chars
            {
                XmlDocument file = new XmlDocument();
                try {
                    file.Load(LangFileXML);  
                } catch(Exception ex){ 
                    MessageBox.Show("Language file is corrupted/Jazykový soubor je poškozen\r\nCheck file \"Lang.xml\"\r\n\r\nDetails/Podrobnosti:\r\n"+ex.Message,"Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
                string letters=charsEverytime;

                foreach(XmlNode node in file.DocumentElement.ChildNodes) {
                    if (node.Name=="Langs") { 
                        foreach (XmlNode n in node.ChildNodes) {
                            if (n.Attributes["EnglishName"].Value==null) continue;
                            if (n.Attributes["NativeName"].Value==null) continue;
                           
                            if (n.Attributes["alphabet"]!=null){ 
                                foreach (char ch in n.Attributes["alphabet"].Value){ 
                                    bool exists=false;
                                    foreach (char ch2 in letters){ 
                                        if (ch==ch2) {
                                            exists=true;
                                            break; 
                                        }
                                    }
                                    if (!exists) letters+=ch;
                                   
                                }
                            }
                        }
                        break;
                    }
                }

                foreach (char ch2 in letters){ 
                    bool exists=false;

                    foreach (LangFile l in LangFiles) {
                        foreach (SavedChar ch in l.chars) {
                            if (ch.Code==(int)ch2){
                                exists=true;
                                break;
                            }
                        }
                        if (exists)break;
                    }
                    if (!exists){ 
                        Debug.WriteLine("Nepodporovaný znak: '"+ch2+"'; "+(int)ch2);    
                    }
                }
            }

            #endregion

            
            NativeMethods.ColorImageAttributes();
         
            DateTime dt=DateTime.Now;

            int totalw=0;
            int maxHBasic=0;
            int averageH=0, averageW=0;
            List<string> files=new List<string>();

            foreach (LangFile LF in LangFiles){ 
               // Console.Title="Font "++" "+LF.name;
                Console.WriteLine("Tvorba písma "+LF.name+" "+ss+" ");
                var chars=LF.chars;



            var families = Fonts.GetFontFamilies(LF.fontFile);

            System.Windows.Media.FontFamily family =families.ToList()[0];
            LF.font=new Font(family.FamilyNames.ToArray()[0].Value, fontsize);



                 Console.WriteLine("    Vytváření znaků");
                 Console.WriteLine("    0%");
            for (int i=0; i<chars.Count; i++) {
                if (i%20==0) {
                    if ((DateTime.Now-dt).TotalSeconds>1) {
                        dt=DateTime.Now;
  
                    //    Program.add=i/(float)chars.Count;
                    //    Program.ReDrawGraph();

                      // Console.CursorLeft=2;
                    //    Console.CursorTop=Program.LinePercentage;
                     if (((int)(i/(float)chars.Count*100f))!=0 && ((int)(i/(float)chars.Count*100f))!=100)  Console.WriteLine("    "+((int)(i/(float)chars.Count*100f))+"%");
                       //   Debug.WriteLine(((int)(i/(float)chars.Count*100f))+"%");
                    }
                }

                string s=((char)chars[i].Code).ToString();

                using (Bitmap image=new Bitmap(100,100)) {
                    using (Graphics g =Graphics.FromImage(image)) {
                              
                        SizeF size=g.MeasureString(s,LF.font,10000,StringFormat.GenericTypographic);
                     
                        int h=(int)size.Height, w=(int)size.Width;
                        if (w<=1)w=10;
                        totalw+=w;

                        h+=10;
                        if (maxHBasic<h)maxHBasic=h;
                        averageH+=h;
                        averageW+=w;

                      //  SizeF m=g.MeasureString(s,font);

                        {
                            (Bitmap, RectangleF, int, int) text= NativeMethods.Text(LF.font,s);
                            Bitmap bm=text.Item1;
                            if (bm==null){ 
                                // Mezera se těžce počítá - dej něco na kraje
                                SizeF size2=g.MeasureString("."+s+".",LF.font, 10000, StringFormat.GenericTypographic);
                                SizeF size3=g.MeasureString("..",LF.font, 10000, StringFormat.GenericTypographic);
                                chars[i].Saved=false;
                                chars[i].W=(int)(size2.Width-size3.Width);
                                chars[i].H=(int)(size2.Height-size3.Height);
                            }else{
                                Rectangle rec=new Rectangle(Left(bm), Top(bm), Right(bm), Down(bm));

                                    //if (rec.X==text.Item2.X
                                    //     &&rec.Width==text.Item2.Width
                                    //     &&rec.Y==text.Item2.Y
                                    //     &&rec.Height==text.Item2.Height) {
                                    //    SavedChar ch=chars[i];
                                    //    ch.Saved=true;

                                    //    ch.bitmap=bm;
                                    //ch.r=text.Item3;
                                    //    ch.X=(int)text.Item2.X;
                                    //    ch.Y=(int)text.Item2.Y;

                                    //    ch.W=(int)(/*(int)m.Width-*/text.Item2.Width-bm.Width-text.Item3-text.Item4*2);/*w-br.Item2.Width-br.Item2.X;*/
                                    //    ch.H=(int)(/*(int)m.Height-*/text.Item2.Height-bm.Height-text.Item3-text.Item4*2);/*h-br.Item2.Height-br.Item2.Y*/;
                                    //} else {
                                        Bitmap toSave = new Bitmap(rec.Width-rec.X, rec.Height-rec.Y);
                                        using (Graphics gg = Graphics.FromImage(toSave)) {
                                            gg.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                          //  gg.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                                            gg.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                            gg.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                                        gg.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                            gg.DrawImage(bm, 0, 0, rec, GraphicsUnit.Pixel);
                                            #if DEBUG
                                            gg.DrawRectangle(new System.Drawing.Pen(System.Drawing.Brushes.Green), 0, 0, toSave.Width-1, toSave.Height-1);
                                            #endif
                                        }
                                       //
                                        SavedChar ch = chars[i];
                                        ch.Saved=true;

                                        ch.bitmap=toSave;
                                        
                                        float removedX=rec.X-text.Item3/2f, removedY=rec.Y-text.Item3/2f;
                                    
                                           ch.X=(int)/*Math.Round*/((/*(*/text.Item2.X/*-text.Item3)*/+removedX/2f)/*+rec.X*//**/);/*(int)(text.Item2.X-rec.X)*/
                                        ch.Y=(int)/*Math.Round*/((/*(*/text.Item2.Y/*-text.Item3)*/+removedY/2f)/*+rec.Y*//*Y*/);/*(int)(text.Item2.Y-rec.Y);*/

                                        //  ch.X=(int)Math.Round(text.Item2.X-text.Item3+rec.X);/*(int)(text.Item2.X-rec.X)*/
                                        //ch.Y=(int)Math.Round(text.Item2.Y-text.Item3+rec.Y);/*(int)(text.Item2.Y-rec.Y);*/
                                   // ch.r
                                           ch.W=(text.Item1.Width-rec.Width-text.Item3-rec.X)-2;//(int)(/*(int)m.Width-*/ text.Item2.Width-bm.Width-text.Item3-text.Item4*2-(bm.Width-rec.Width-rec.X);/*w-br.Item2.Width-br.Item2.X;*/
                                        ch.H=(text.Item1.Height-rec.Height-text.Item3-rec.Y)-1;//(int)(/*(int)m.Height-*/text.Item2.Height-bm.Height-text.Item3-text.Item4*2-(bm.Height-rec.Height-rec.Y));/*h-br.Item2.Height-br.Item2.Y*/;
                                       bm.Dispose();
                                 //   }

                                }
           

           // return (newBitmap,rec, true);
                            }
                        //using (Bitmap imageOut=new Bitmap(w+4, h+4+20)){
                        //        Rectangle area;
                        //    using (Graphics go =Graphics.FromImage(imageOut)){
                        //      area= NativeMethods.Text(/*go,*/font,s,(int)(w/2f-m.Width/2f),(int)(h/2f-m.Height/2f));
                        //    }   

                        //    {
                        //        //    Bitmap bmp=null;
                        //        //    if (area.Width!=0){
                        //        // bmp=new Bitmap(area.Width,area.Height);
                        //        //using (Graphics gg=Graphics.FromImage(bmp)){ 
                        //        //    gg.DrawImage(imageOut,new Rectangle(0,0,area.Width,area.Height),new Rectangle(area.X-1,area.Y-1,area.Width+5,area.Height+5),GraphicsUnit.Pixel);
                        //        //}
                        //        //    }
                        //        (Bitmap, Rectangle, bool) br=CropImage(imageOut);
                        //       // (Bitmap, Rectangle, bool) br=(bmp,area,bmp!=null);
                        //        if (br.Item3){
                        //            SavedChar ch=chars[i];
                        //            ch.Saved=true;

                        //            ch.bitmap=br.Item1;

                        //            ch.X=br.Item2.X;
                        //            ch.Y=br.Item2.Y;

                        //            ch.W=w-br.Item2.Width-br.Item2.X;
                        //            ch.H=h-br.Item2.Height-br.Item2.Y;
                        //        } else { 

                        //            // Mezera se těžce počítá - dej něco na kraje
                        //            SizeF size2=g.MeasureString("."+s+".",font, 10000, StringFormat.GenericTypographic);
                        //            SizeF size3=g.MeasureString("..",font, 10000, StringFormat.GenericTypographic);
                        //            chars[i].Saved=false;
                        //            chars[i].W=(int)(size2.Width-size3.Width);
                        //            chars[i].H=(int)(size2.Height-size3.Height);
                        //        }
                        //    }
                       // }
                    }
                }
            }
            Console.WriteLine("    100%");
            Console.WriteLine("    Přeuspořádání");
            //Od nejvyššího
            IOrderedEnumerable<SavedChar> sch_= chars.OrderBy(i=> i.bitmap?.Height);
            
            chars=sch_.ToList();
            chars.Reverse();      
            
            int sss=0;
            foreach (SavedChar sch in chars){ if (sch.Saved) sss+=sch.bitmap.Width*sch.bitmap.Height;}
            averageH/=chars.Count;
            averageW/=chars.Count;

            int aw=(int)Math.Sqrt(sss);
            int ah=(aw/maxHBasic+5)*maxHBasic;

            byte[]outputinfo=new byte[(4+2+2+1+1+1+1+1+1)*chars.Count];
            int outputinfoPos=0;
            int PosX=0, PosY=0;

            Console.WriteLine("    Tvorba výsledné bitmapy");
            
            List<SavingBytes> ListSavingBytes=new List<SavingBytes>();
                            
            using (Bitmap extra=new Bitmap(aw,ah)){ 
                using (Graphics g=Graphics.FromImage(extra)){
                    g.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    
                    int lineHeight=0;

                    for (int i=0; i<chars.Count; i++){
                        //if ((DateTime.Now-dt).TotalSeconds>1){
                        //    dt=DateTime.Now;
                          
                        
                        //    Program.add=i/(float)chars.Count;
                           
                        //    Program.ReDrawGraph(); 
                        //    Console.CursorLeft=2;
                        //    Console.CursorTop=Program.LinePercentage;
                        //    Console.WriteLine(((int)(i/(float)chars.Count*100f))+"%");
                        //}

                        SavedChar sch=chars[i];
                        int ch = sch.Code;
                       
                        if (!sch.placedOnSide){
                            if (sch.Saved) {  
                                Image b =sch.bitmap;
                                if (PosX>aw-b.Width){ 

                                    int best=10000000;
                                    int bestIndex=-1;

                                    for (int ii=i; ii<chars.Count; ii++) {
                                        SavedChar ch2=chars[ii];
                                        if (ch2.Saved && ch2.bitmap!=null && !ch2.placedOnSide) {  
                                            if (aw-PosX-ch2.bitmap.Width>0){ 
                                                int area=(aw-PosX)*lineHeight-ch2.bitmap.Width*ch2.bitmap.Height;
                                                if (area<best){
                                                    best=area;
                                                    bestIndex=ii;
                                                }
                                            } 
                                        }
                                    }
                                    
                                    if (bestIndex!=-1) { 
                                        SavedChar ch2=chars[bestIndex];
                                        ch2.placedOnSide=true;

                                        g.DrawImage(ch2.bitmap, new Point(PosX, PosY));

                                        SavingBytes sb = new SavingBytes {
                                            code=ch2.Code
                                        };

                                        ushort pX=(ushort)PosX,pY=(ushort)PosY;
                                        if (ch2.W<0)ch2.W=0;
                                        if (ch2.H<0)ch2.H=0;
                                        uint uch=(uint)ch2.Code;
                                        sb.bytes=new byte[]{
                                            outputinfo[outputinfoPos]=(byte)(uch>>24),
                                            outputinfo[outputinfoPos+1]=(byte)(uch>>16),
                                            outputinfo[outputinfoPos+2]=(byte)(uch>>8),
                                            outputinfo[outputinfoPos+3]=(byte)uch,
                                            1,

                                            (byte)(pX>>8),
                                            (byte)pX,

                                            (byte)(pY>>8),
                                            (byte)pY,

                                            (byte)(ch2.bitmap.Width/*-ch2.r*/),
                                            (byte)(ch2.bitmap.Height/*-ch2.r*/),

                                            (byte)(ch2.X/*-1*/+128),//?
                                            (byte)(ch2.Y/*-1*/+128),//?

                                     
                                            (byte)ch2.W,
                                            (byte)ch2.H 
                                        };
                                        ListSavingBytes.Add(sb);

                                        ch2.bitmap.Dispose();
                                        ch2.bitmap=null;
                                    }

                                    PosY+=lineHeight;
                                    lineHeight=0;
                                    PosX=0;
                                }

                                { 
                                    g.DrawImage(b,new Point(PosX,PosY));
                                    uint uch=(uint)ch;
                             
                                    ushort pX=(ushort)PosX,pY=(ushort)PosY;
                                 
                                    if (sch.W<0)sch.W=0;
                                    if (sch.H<0)sch.H=0;
                                  
                                    SavingBytes sb = new SavingBytes {
                                        code=ch
                                    };
                                     
                                    sb.bytes=new byte[]{
                                        outputinfo[outputinfoPos]=(byte)(uch>>24),
                                        outputinfo[outputinfoPos+1]=(byte)(uch>>16),
                                        outputinfo[outputinfoPos+2]=(byte)(uch>>8),
                                        outputinfo[outputinfoPos+3]=(byte)uch,
                                                                                       
                                        1,

                                        (byte)(pX>>8),
                                        (byte)pX,

                                        (byte)(pY>>8),
                                        (byte)pY,

                                        (byte)(sch.bitmap.Width/*-sch.r*/),
                                        (byte)(sch.bitmap.Height/*-sch.r*/),

                                        (byte)(sch.X/*-1*/+128),
                                        (byte)(sch.Y/*-1*/+128),

                                     
                                        (byte)sch.W,
                                        (byte)sch.H 
                                    };
                                    ListSavingBytes.Add(sb);
  
                                    PosX+=b.Width+1;
                                    if (lineHeight<b.Height)lineHeight=b.Height;
                                    b.Dispose();   
                                }
                            } else {
                                SavingBytes sb = new SavingBytes {
                                    code=ch
                                };

                                uint uch=(uint)ch;
                                sb.bytes=new byte[]{
                                    outputinfo[outputinfoPos]=(byte)(uch>>24),
                                    outputinfo[outputinfoPos+1]=(byte)(uch>>16),
                                    outputinfo[outputinfoPos+2]=(byte)(uch>>8),
                                    outputinfo[outputinfoPos+3]=(byte)uch,
                                            
                                    0,

                                    (byte)sch.W,
                                    (byte)sch.H 
                                };

                                ListSavingBytes.Add(sb);
                            }

                            outputinfoPos+=14;
                        } 
                    }
                }
              
                Console.WriteLine("    Ukládání");  
            
                {
                     Size rec=new Size(Right(extra)+1, Down(extra)+2);
         
                    using (Bitmap newBitmap = new Bitmap(rec.Width, rec.Height)){
                        using (Graphics g = Graphics.FromImage(newBitmap)) {
                            g.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.DrawImage(extra, 0, 0);
                        }    

                        //try 
                        BitmapData bd=newBitmap.LockBits(new Rectangle(0,0,newBitmap.Width,newBitmap.Height),ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        int len=(int)bd.Scan0+newBitmap.Width*newBitmap.Height*4;
                        for (int i=(int)bd.Scan0; i<len; i++){ 
                            byte* x=(byte*)i;
                            int n=((int)(*x/(float)LF.quality+0.5f))*LF.quality;
                            if (n>255)*x=255;
                            else *x=(byte)(n);
                        }
                        newBitmap.UnlockBits(bd);
                        newBitmap.Save(BitmapTexturePath+"\\Font "+LF.name+" "+LF.font.Size+".png", ImageFormat.Png);
                    }
                }

                Console.WriteLine("");  
            }
            {
                IOrderedEnumerable<SavingBytes> list= ListSavingBytes.OrderBy(i=> i.code);
                List<byte> bytes=new List<byte>();
                foreach (SavingBytes sb in list){ 
                    bytes.AddRange(sb.bytes);
                }

                // Save position of chars and things like that...
                File.WriteAllBytes(FontInfo+"\\FontInfo "+LF.name+" "+LF.font.Size+".bin",bytes.ToArray());
            }

           // Program.DrawGraph("Konec");
            }
        }

        static readonly char[] selfishArabicCharsL={' ', 'و', 'ز', 'ر', 'ذ', 'د', 'ا' ,/*?*/'ۇ','\uFEFB','ە'};//
        static readonly char[] selfishArabicCharsR={' ', /*'و',*/ 'ز', 'ر', 'ذ', 'د', 'ا', /*?'ۇ',*/'ي'};
        static readonly char[] arabicDisconnecters={'\uFC62', '\u064B', '\u064C', '\u064D', '\u064E', '\u064F', '\u0650', '\u0651', '\u0652', '\u0653','\u0654','\u0655'};

        static bool IsSelfishL(char ch) {
            foreach (char c in selfishArabicCharsL) {
                if (c==ch) return true;
            }
            return false;
        }

        static bool IsSelfishR(char ch){
            foreach (char c in selfishArabicCharsR) {
                if (c==ch)return true;
            }
            return false;
        }

        static bool IsAsrabicDisconecter(char ch) {
            foreach (char c in arabicDisconnecters) {
                if (c==ch) return true;
            }
            return false;
        }

        static int ArabicFinalFormConventer(int s) {
            switch (s) { 
                case 1609: return 65264;
                case 1577: return 65172;
                case 1570: return 65154;
                case 1610: return 65266;
                case 1578: return 65174;
                case 1594: return 65230;
                case 1576: return 65168;
                case 1604: return 65246;
                case 1583: return 65193;
                case 1601: return 65234;
                case 1606: return 65254;
                case 1593: return 65226;
                case 1605: return 65250;   
                case 1585: return 65198;
                case 1603: return 65242;
                case 1749: return 65258;//?
                case 1574: return 65162;
                case 1735: return 64472;
                case 1670: return 64379;
                case 1575: return 65166;
                case 1579: return 65178;
                case 1580: return 65182;
                case 1581: return 65186;
                case 1582: return 65190;
                case 1584: return 65196;
                case 1586: return 65200;
                case 1587: return 65202;
                case 1588: return 65206;
                case 1589: return 65210;
                case 1590: return 65214;
                case 1591: return 65218;
                case 1592: return 65222;
                case 1602: return 65238;
                case 1607: return 65258;
                case 1608: return 65262;
                case 1571: return 65156;
                case 1573: return 65160;
                case 1572: return 65158;
                case 1709: return 64468;
                case 1736: return 64476;
            }
            return s;
        }

        static int ArabicInitialFormConventer(int s) {
            switch (s) { 
                case 1578: return 65175;
                case 1594: return 65231;
                case 1574: return 65163;
                case 1610: return 65267;
                case 1576: return 65169;
                case 1601: return 65235;
                case 1606: return 65255;
                case 1593: return 65227;
                case 1605: return 65251;
                case 1603: return 65243;
                case 1670: return 64380;
                case 1579: return 65179;
                case 1580: return 65183;
                case 1581: return 65187;
                case 1582: return 65191;
                case 1587: return 65203;
                case 1588: return 65207;
                case 1589: return 65211;
                case 1590: return 65215;
                case 1591: return 65219;
                case 1592: return 65223;
                case 1602: return 65239;
                case 1604: return 65247;
                case 1607: return 65259;
                case 1709: return 64469;
                case 1609: return 64488;
            }
            return s;
        }
        
        static int ArabicMedialFormConventer(int s) {
            switch (s) { 
                case 1610: return 65268;
                case 1578: return 65176;
                case 1604: return 65248;
                case 1594: return 65232; 
                case 1609: return 64489;
                case 1576: return 65170;
                case 1601: return 65236;
                case 1606: return 65256;
                case 1593: return 65228;
                case 1605: return 65252;
                case 1603: return 65244;
                case 1574: return 65164;
                case 1670: return 64381;
                case 1575: return 65166;
                case 1579: return 65180;
                case 1580: return 65184;
                case 1581: return 65188;
                case 1582: return 65192;
                case 1583: return 65193;
                case 1584: return 65196;
                case 1585: return 65198;
                case 1586: return 65200;
                case 1587: return 65204;
                case 1588: return 65208;
                case 1589: return 65212;
                case 1590: return 65216;
                case 1591: return 65220;
                case 1592: return 65224;
                case 1602: return 65240;
                case 1607: return 65260;
                case 1608: return 65262;
                case 1571: return 65156;
                case 1573: return 65160;
                case 1572: return 65158;
                case 1570: return 65154;
                case 1735: return 64472;
                case 1749: return 65258;//?
                case 1709: return 64470;
                case 1736: return 64476;
            }
            return s;
        }

        // Crop Image
        static (Bitmap, Rectangle, bool) CropImage(Bitmap bm) { 
            Rectangle rec=new Rectangle(Left(bm), Top(bm), Right(bm)+1, Down(bm)+1);

            if (rec.X==-1) return (null,rec, false);
            rec.Width-=rec.X;
            rec.Height-=rec.Y;

            Bitmap newBitmap = new Bitmap(rec.Width, rec.Height);
            using (Graphics g = Graphics.FromImage(newBitmap)) {
                g.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                g.DrawImage(bm, 0, 0, rec, GraphicsUnit.Pixel);
            }

            return (newBitmap,rec, true);
        }

        // Get amount of transparent
        static int Left(Bitmap b) { 
            for (int x = 0; x < b.Width; x++) {
                for (int y = 0; y < b.Height; y++) {
                    if (b.GetPixel(x,y).A != 0) return x; 
                }
            }
            return -1;
        }
        static int Top(Bitmap b){ 
            for (int y = 0; y < b.Height; y++) {
                for (int x = 0; x < b.Width; x++) {
                    if (b.GetPixel(x, y).A != 0) return y;
                }
            }   
            return -1;
        }

        static int Down(Bitmap b){ 
            for (int y =  b.Height-1; y>0; y--) {
                for (int x = b.Width-1; x>0 ; x--) {
                    if (b.GetPixel(x, y).A != 0) return y;
                }
            }   
            return -1;
        }

        static int Right(Bitmap b) {
            for (int x = b.Width-1; x>0 ; x--) {
                for (int y =  b.Height-1; y>0; y--) {
                    if (b.GetPixel(x, y).A != 0) return x;
                }
            }   
            return -1;
        }

        // store all characters in list
        void Support() { 
            foreach (LangFile f in LangFiles) { 
          //  Console.WriteLine(f.name+" "+f.fontFile);
                var families = Fonts.GetFontFamilies(f.fontFile);
                System.Windows.Media.FontFamily family =families.ToList()[0];

                var typefaces = family.GetTypefaces();
                Typeface typeface = typefaces.ToArray()[0];

                typeface.TryGetGlyphTypeface(out GlyphTypeface glyph);
                IDictionary<int, ushort> characterMap = glyph.CharacterToGlyphMap;
         
                foreach (KeyValuePair<int, ushort> kvp in characterMap) {
                    int ChCode=kvp.Key;
                    bool save=true;

                    foreach (string s in donotsave){ 
                        if (((char)ChCode).ToString()==s) save=false;
                    }
                
                    if (save) { 
                        foreach (FromTo ft in f.Range) {
                            if (ft.From<=ChCode) { 
                                if (ft.To>=ChCode) { 
                                    f.chars.Add(new SavedChar { Code=ChCode });
                                    break;
                                }
                            }
                        } 
                    }
                } 
            }
        }

        public void Dispose() { }

        public List<FromTo> JapaneseRange(){ 
            string chars=File.ReadAllText(allkanjitorange/*(@"C:\Users\GeftGames\Programování\FontTextureCreator\FontTextureCreator\allkanjitorange.txt"*/);
            List<FromTo> list=new List<FromTo>();

            foreach (char ch in chars) AddChar(ch);

             void AddChar(int c) { 
                // Zkontroluj zda již znak existuje v seznamu
                foreach (FromTo x in list) {
                    if (x.From<=c) {
                        if (x.To>=c) {
                            return;
                        }
                    }
                }
                   
                //Přidat znak
                list.Add(new FromTo {
                    From=c,
                    To=c,
                });
            }
            return list;
        }
    }
}