using System.Collections.Generic;
using System.Drawing;

namespace FontTextureCreator {
    class LangFile { 
        public List<SavedChar> chars;
        public FromTo[] Range;
        public List<FromTo> BuildingRange;
        public string name;
        public int quality;//255 max, 2 min

        public List<string> languages;

        public string fontFile;
        public Font font;
    }

    class FromTo { 
        public int From, To;    
    }

    class SavingBytes{ 
        public int code;
        public byte[] bytes;
    }

    class SavedChar { 
        public int Code;
        public int X, Y, W, H;
        public bool Saved;

        public Bitmap bitmap;
      //  public int r;
       // public int Area{ get{ if (bitmap!=null){ return bitmap.Width*bitmap.Height;}return 0; } }

        public bool placedOnSide;
    }
}