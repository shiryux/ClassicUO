﻿using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.AssetsLoader
{
    public enum TEXT_ALIGN_TYPE
    {
        TS_LEFT = 0,
        TS_CENTER,
        TS_RIGHT
    }

    public enum HTML_TAG_TYPE
    {
        HTT_NONE = 0,
        HTT_B,
        HTT_I,
        HTT_A,
        HTT_U,
        HTT_P,
        HTT_BIG,
        HTT_SMALL,
        HTT_BODY,
        HTT_BASEFONT,
        HTT_H1,
        HTT_H2,
        HTT_H3,
        HTT_H4,
        HTT_H5,
        HTT_H6,
        HTT_BR,
        HTT_BQ,
        HTT_LEFT,
        HTT_CENTER,
        HTT_RIGHT,
        HTT_DIV
    }

    public static class Fonts
    {
      
        const int UOFONT_SOLID = 0x01;
        const int UOFONT_ITALIC = 0x02;
        const int UOFONT_INDENTION = 0x04;
        const int UOFONT_BLACK_BORDER = 0x08;
        const int UOFONT_UNDERLINE = 0x10;
        const int UOFONT_FIXED = 0x20;
        const int UOFONT_CROPPED = 0x40;
        const int UOFONT_BQ = 0x80;

        const int UNICODE_SPACE_WIDTH = 8;
        const int MAX_HTML_TEXT_HEIGHT = 18;
        const float ITALIC_FONT_KOEFFICIENT = 3.3f;

        public static int FontCount { get; private set; }

        private static FontData[] _font;
        private static readonly IntPtr[] _unicodeFontAddress = new IntPtr[20];
        private static readonly long[] _unicodeFontSize = new long[20];
        private static readonly Dictionary<ushort, WebLink> _webLinks = new Dictionary<ushort, WebLink>();

        public static bool UnusePartialHue { get; set; } = false;
        public static bool RecalculateWidthByInfo { get; set; } = false;
        public static bool IsUsingHTML => _useHTML;

        private static readonly int[] offsetCharTable = { 2, 0, 2, 2, 0, 0, 2, 2, 0, 0 };
        private static readonly int[] offsetSymbolTable = { 1, 0, 1, 1, -1, 0, 1, 1, 0, 0 };
        private static readonly byte[] _fontIndex =  
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0,    1,    2,    3,    4,    5,    6,    7,    8,    9,    10,   11,   12,   13,   14,   15,
            16,   17,   18,   19,   20,   21,   22,   23,   24,   25,   26,   27,   28,   29,   30,   31,
            32,   33,   34,   35,   36,   37,   38,   39,   40,   41,   42,   43,   44,   45,   46,   47,
            48,   49,   50,   51,   52,   53,   54,   55,   56,   57,   58,   59,   60,   61,   62,   63,
            64,   65,   66,   67,   68,   69,   70,   71,   72,   73,   74,   75,   76,   77,   78,   79,
            80,   81,   82,   83,   84,   85,   86,   87,   88,   89,   90,   0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 136,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 152,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            160,  161,  162,  163,  164,  165,  166,  167,  168,  169,  170,  171,  172,  173,  174,  175,
            176,  177,  178,  179,  180,  181,  182,  183,  184,  185,  186,  187,  188,  189,  190,  191,
            192,  193,  194,  195,  196,  197,  198,  199,  200,  201,  202,  203,  204,  205,  206,  207,
            208,  209,  210,  211,  212,  213,  214,  215,  216,  217,  218,  219,  220,  221,  222,  223
        };

        private static uint _webLinkColor = 0;
        private static uint _visitedWebLinkColor = 0;
        private static uint _backgroundColor = 0;
        private static int _leftMargin = 0, _topMargin = 0, _rightMargin = 0, _bottomMargin = 0;
        private static bool _useHTML = false;
        private static bool _HTMLBackgroundCanBeColored = false;
        private static uint _HTMLColor = 0xFFFFFFFF;

        public static void Load()
        {
            UOFileMul fonts = new UOFileMul(Path.Combine(FileManager.UoFolderPath, "fonts.mul"));

            UOFileMul[] uniFonts = new UOFileMul[20];
            for (int i = 0; i < 20; i++)
            {
                string path = Path.Combine(FileManager.UoFolderPath, "unifont" + (i == 0 ? "" : i.ToString()) + ".mul");
                if (File.Exists(path))
                {
                    uniFonts[i] = new UOFileMul(path);
                    _unicodeFontAddress[i] = uniFonts[i].StartAddress;
                    _unicodeFontSize[i] = uniFonts[i].Length;
                }
            }

            int fontHeaderSize = Marshal.SizeOf<FontHeader>();
            FontCount = 0;

            while (fonts.Position < fonts.Length)
            {
                bool exit = false;
                fonts.Skip(1);

                unsafe
                {
                    for (int i = 0; i < 224; i++)
                    {
                        FontHeader* fh = (FontHeader*)fonts.PositionAddress;
                        fonts.Skip(fontHeaderSize);

                        int bcount = fh->Width * fh->Height * 2;
                        if (fonts.Position + bcount > fonts.Length)
                        {
                            exit = true;
                            break;
                        }

                        fonts.Skip(bcount);
                    }
                }

                if (exit)
                    break;

                FontCount++;
            }

            if (FontCount < 1)
            {
                FontCount = 0;
                return;
            }

            _font = new FontData[FontCount];
            fonts.Seek(0);

            for (int i = 0; i < FontCount; i++)
            {
                _font[i].Header = fonts.ReadByte();
                _font[i].Chars = new FontCharacterData[224];
                for (int j = 0; j < 224; j++)
                {
                    _font[i].Chars[j].Width = fonts.ReadByte();
                    _font[i].Chars[j].Height = fonts.ReadByte();
                    fonts.Skip(1);
                    int dataSize = _font[i].Chars[j].Width * _font[i].Chars[j].Height;
                    _font[i].Chars[j].Data = fonts.ReadArray<ushort>(dataSize).ToList();
                }
            }


            if (_unicodeFontAddress[1] == IntPtr.Zero)
            {
                _unicodeFontAddress[1] = _unicodeFontAddress[0];
                _unicodeFontSize[1] = _unicodeFontSize[0];
            }

            for (int i = 0; i < 256; i++)
            {
                if (_fontIndex[i] >= 0xE0)
                    _fontIndex[i] = _fontIndex[' '];
            }
        }


        private static int GetWidthASCII(in byte font, in string str)
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
                return 0;
            FontData fd = _font[font];
            int textLength = 0;
            foreach (char c in str)
                textLength += fd.Chars[_fontIndex[(byte)c]].Width;
            return textLength;
        }

        private static int GetHeightASCII(MultilinesFontInfo info)
        {
            int textHeight = 0;

            while (info != null)
            {
                textHeight += info.MaxHeight;
                info = info.Next;
            }
            return textHeight;
        }

        public static (uint[], int, int, int, bool) GenerateASCII(in byte font, in string str, in ushort color, int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            int linesCount = 0;
            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0)
            {
                linesCount--;
                if (width <= 0 || string.IsNullOrEmpty(str))
                    return (null, 0, 0, linesCount, false);

                int realWidth = GetWidthASCII(font, str);

                if (realWidth > width)
                {
                    string newstr = GetTextByWidthASCII(font, str, width, (flags & UOFONT_CROPPED) != 0);
                    return GeneratePixelsASCII(font, newstr, color, width, align, flags);
                }
            }

            return GeneratePixelsASCII(font, str, color, width, align, flags);
        }

        private static string GetTextByWidthASCII(in byte font, in string str, int width, in bool isCropped)
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
                return string.Empty;

            FontData fd = _font[font];

            if (isCropped)
                width -= fd.Chars[_fontIndex[(byte)'.']].Width * 3;

            int textLength = 0;
            string result = "";

            foreach (char c in str)
            {
                textLength += fd.Chars[_fontIndex[(byte)c]].Width;
                if (textLength > width)
                    break;
                result += c;
            }

            if (isCropped)
                result += "...";

            return result;
        }

        private static (uint[], int, int, int, bool) GeneratePixelsASCII(in byte font, in string str, in ushort color, int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            uint[] pData;

            if (font >= FontCount)
                return (null, 0, 0, 0, false);

            int len = str.Length;
            if (len <= 0)
                return (null, 0, 0, 0, false);

            FontData fd = _font[font];
            if (width <= 0)
                width = GetWidthASCII(font, str);
            if (width <= 0)
                return (null, 0, 0, 0, false);

            MultilinesFontInfo info = GetInfoASCII(font, str, len, align, flags, width);
            if (info == null)
                return (null, 0, 0, 0, false);

            width += 4;
            int height = GetHeightASCII(info);

            if (height <= 0)
            {
                MultilinesFontInfo ptr1 = info;
                while (ptr1 != null)
                {
                    info = ptr1;
                    ptr1 = ptr1.Next;
                    info.Data.Clear();
                    info = null;
                }

                return (null, 0, 0, 0, false);
            }

            int blocksize = height * width;
            pData = new uint[blocksize];

            int lineOffsY = 0;
            MultilinesFontInfo ptr = info;

            bool partialHue = (font != 5 && font != 8) && !UnusePartialHue;
            int font6OffsetY = font == 6 ? 7 : 0;

            int linesCount = 0; // this value should be added to TextTexture.LinesCount += linesCount

            while (ptr != null)
            {
                info = ptr;
                linesCount++;
                int w = 0;
                if (ptr.Align == TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    w = (((w - 10) - ptr.Width) / 2);
                    if (w < 0)
                        w = 0;
                }
                else if (ptr.Align == TEXT_ALIGN_TYPE.TS_RIGHT)
                {
                    w = ((width - 10) - ptr.Width);
                    if (w == 0)
                        w = 0;
                }
                else if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                    w = ptr.IndentionOffset;

                int count = ptr.Data.Count;

                for (int i = 0; i < count; i++)
                {
                    byte index = (byte)ptr.Data[i].Item;
                    int offsY = GetFontOffsetY(font, index);

                    FontCharacterData fcd = fd.Chars[_fontIndex[index]];
                    int dw = fcd.Width;
                    int dh = fcd.Height;

                    ushort charColor = color;

                    for (int y = 0; y < dh; y++)
                    {
                        int testrY = y + lineOffsY + offsY;
                        if (testrY >= height)
                            break;

                        for (int x = 0; x < dw; x++)
                        {
                            if (x + w >= width)
                                break;
                            ushort pic = fcd.Data[(y * dw) + x];

                            if (pic > 0)
                            {
                                uint pcl = 0;

                                if (partialHue)
                                    pcl = Hues.GetPartialHueColor(pic, charColor);
                                else
                                    pcl = Hues.GetColor(pic, charColor);

                                int block = (testrY * width) + x + w;

                                pData[block] = Hues.RgbaToArgb((pcl << 8) | 0xFF);
                            }
                        }
                    }

                    w += dw;
                }

                lineOffsY += (ptr.MaxHeight - font6OffsetY);
                ptr = ptr.Next;
                info.Data.Clear();
                info = null;
            }

            return (pData, width, height, linesCount, partialHue);
        }
  
        public static int GetFontOffsetY(in byte font, in byte index)
        {
            if (index == 0xB8)
                return 1;
            else if (!(index >= 0x41 && index <= 0x5A) && !(index >= 0xC0 && index <= 0xDF) && index != 0xA8)
            {
                if (font < 10)
                {
                    if (index >= 0x61 && index <= 0x7A)
                        return offsetCharTable[font];
                    return offsetSymbolTable[font];
                }
                else
                    return 2;
            }
            return 0;
        }

        public static MultilinesFontInfo GetInfoASCII(in byte font, in string str, in int len, in TEXT_ALIGN_TYPE align, in ushort flags, in int width)
        {
            if (font >= FontCount)
                return null;
            FontData fd = _font[font];

            MultilinesFontInfo info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;

            MultilinesFontInfo ptr = info;

            int indentionOffset = 0;
            ptr.IndentionOffset = 0;

            bool isFixed = (flags & UOFONT_FIXED) != 0;
            bool isCropped = (flags & UOFONT_CROPPED) != 0;

            int charCount = 0;
            int lastSpace = 0;
            int readWidth = 0;

            for (int i = 0; i < len; i++)
            {
                char si = str[i];
                if (si == '\r' || si == '\n')
                {
                    if (si == '\r' || isFixed || isCropped)
                        continue;
                    si = '\n';
                }

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                }

                FontCharacterData fcd = fd.Chars[_fontIndex[(byte)si]];

                if (si == '\n' || ptr.Width + readWidth + fcd.Width > width)
                {
                    if (lastSpace == ptr.CharStart && lastSpace <= 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        lastSpace = i;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        ptr.Data.Resize(ptr.CharCount); // = new List<MultilinesFontData>(ptr.CharCount);
                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i + 1;

                        readWidth = 0;
                        charCount = 0;
                        indentionOffset = 0;

                        ptr.IndentionOffset = 0;
                        continue;
                    }
                    else if (lastSpace + 1 == ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            MultilinesFontData mfd1 = new MultilinesFontData()
                            {
                                Item = si,
                                Flags = flags,
                                Font = font,
                                LinkID = 0,
                                Color = 0xFFFFFFFF
                            };

                            ptr.Data.Add(mfd1);

                            readWidth += fcd.Width;

                            if (fcd.Height > ptr.MaxHeight)
                                ptr.MaxHeight = fcd.Height;
                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;
                        si = str[i];
                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        // DATA.resize() ??????
                        ptr.Data.Resize(charCount); //= new List<MultilinesFontData>(charCount);
                        charCount = 0;

                        if (isFixed || isCropped)
                            break;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();

                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i;
                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;
                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData()
                {
                    Item = si,
                    Flags = flags,
                    Font = font,
                    LinkID = 0,
                    Color = 0xFFFFFFFF
                };
                ptr.Data.Add(mfd);
                readWidth += fcd.Width;
                if (fcd.Height > ptr.MaxHeight)
                    ptr.MaxHeight = fcd.Height;
                charCount++;
            }
            ptr.Width += readWidth;
            ptr.CharCount += charCount;

            if (readWidth <= 0 & len > 0 && (str[len -1 ] == '\n' || str[len - 1] == '\r'))
            {
                ptr.Width = 1;
                ptr.MaxHeight = 14;
            }

            if (font == 4)
            {
                ptr = info;
                while (ptr != null)
                {
                    if (ptr.Width > 1)
                        ptr.MaxHeight = ptr.MaxHeight + 2;
                    else
                        ptr.MaxHeight = ptr.MaxHeight + 6;
                    ptr = ptr.Next;
                }
            }

            return info;
        }


        public static void SetUseHTML(in bool value, uint htmlStartColor = 0xFFFFFFFF, in bool backgroundCanBeColored = false )
        {
            _useHTML = value;
            _HTMLColor = htmlStartColor;
            _HTMLBackgroundCanBeColored = backgroundCanBeColored;
        }

        

        public static (uint[], int, int, int, List<WebLinkRect>) GenerateUnicode(in byte font, in string str, in ushort color, in byte cell, int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0)
            {
                if (width <= 0 || string.IsNullOrEmpty(str))
                    return (null, 0, 0, 0, null);

                int realWidth = GetWidthUnicode(font, str);

                if (realWidth > width)
                {
                    string newstring = GetTextByWidthUnicode(font, str, width, (flags & UOFONT_CROPPED) != 0);
                    return GeneratePixelsUnicode(font, newstring, color, cell, width, align, flags);
                }
            }

            return GeneratePixelsUnicode(font, str, color, cell, width, align, flags);
        }

        private static unsafe string GetTextByWidthUnicode(in byte font, in string str, int width, in bool isCropped)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return string.Empty;

            uint* table = (uint*)_unicodeFontAddress[font];

            if (isCropped)
            {
                uint offset = table['.'];

                if (offset > 0 && offset != 0xFFFFFFFF)
                    width -= ( *(byte*) ((((IntPtr)table) + (int)offset + 2)) * 3);
            }

            int textLength = 0;
            string result = "";

            foreach (char c in str)
            {
                uint offset = table[c];
                sbyte charWidth = 0;

                if (offset > 0 && offset != 0xFFFFFFFF)
                {
                    byte* ptr = (byte*)(((IntPtr)table) + (int)offset);
                    charWidth = (sbyte)(ptr[0] + ptr[2] + 1);
                }
                else if (c == ' ')
                {
                    charWidth = UNICODE_SPACE_WIDTH;
                }

                if (charWidth > 0)
                {
                    textLength += charWidth;
                    if (textLength > width)
                        break;
                    result += c;
                }
            }

            if (isCropped)
                result += "...";
            return result;
        }

        private static unsafe int GetWidthUnicode(in byte font, in string str)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return 0;

            uint* table = (uint*)_unicodeFontAddress[font];
            int textLength = 0;
            int maxTextLenght = 0;

            foreach (char c in str)
            {
                uint offset = table[c];
                if (offset > 0 && offset != 0xFFFFFFFF)
                {
                    byte* ptr = (byte*)((IntPtr)table + (int)offset);
                    textLength += (ptr[0] + ptr[2] + 1);
                }
                else if (c == ' ')
                    textLength += UNICODE_SPACE_WIDTH;
                else if (c == '\n' || c == '\r')
                {
                    maxTextLenght = Math.Max(maxTextLenght, textLength);
                    textLength = 0;
                }
            }
            return Math.Max(maxTextLenght, textLength);
        }

        private static unsafe MultilinesFontInfo GetInfoUnicode(in byte font, in string str, in int len, in TEXT_ALIGN_TYPE align, in ushort flags, in int width)
        {
            _webLinkColor = 0xFF0000FF;
            _visitedWebLinkColor = 0x0000FFFF;
            _backgroundColor = 0;
            _leftMargin = 0;
            _topMargin = 0;
            _rightMargin = 0;
            _bottomMargin = 0;

            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero)
                return null;

            if (_useHTML)
            {
                return GetInfoHTML(font, str, len, align, flags, width);
            }

            uint* table = (uint*)_unicodeFontAddress[font];
            MultilinesFontInfo info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;

            MultilinesFontInfo ptr = info;

            int indetionOffset = 0;
            ptr.IndentionOffset = 0;

            int charCount = 0;
            int lastSpace = 0;
            int readWidth = 0;

            bool isFixed = ((flags & UOFONT_FIXED) != 0);
            bool isCropped = ((flags & UOFONT_CROPPED) != 0);

            TEXT_ALIGN_TYPE current_align = align;
            ushort current_flags = flags;
            byte current_font = font;
            uint charcolor = 0xFFFFFFFF;
            uint current_charcolor = 0xFFFFFFFF;
            uint lastspace_charcolor = 0xFFFFFFFF;
            uint lastaspace_current_charcolor = 0xFFFFFFFF;

            for (int i = 0; i < len; i++)
            {
                char si = str[i];
                if (si == '\r' || si == '\n')
                {
                    if (isFixed || isCropped)
                        si = (char)0;
                    else
                        si = '\n';
                }

                if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n')
                    continue;

                byte* data = (byte*)(((IntPtr)table) + (int)table[si]);

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                    lastspace_charcolor = charcolor;
                    lastaspace_current_charcolor = current_charcolor;
                }

                if (ptr.Width + readWidth  + (data[0] + data[2] ) > width || si ==  '\n')
                {
                    if (lastSpace == ptr.CharStart && lastSpace <= 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        lastSpace = i;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        ptr.Data.Resize(ptr.CharCount);

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;

                        ptr.Align = current_align;
                        ptr.CharStart = i + 1;

                        readWidth = 0;
                        charCount = 0;
                        indetionOffset = 0;
                        ptr.IndentionOffset = 0;
                        continue;
                    }
                    else if (lastSpace + 1== ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = current_align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (current_flags & UOFONT_INDENTION) != 0)
                        {
                            indetionOffset = 14;
                        }

                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            MultilinesFontData mfd1 = new MultilinesFontData()
                            {
                                Item = si,
                                Flags = current_flags,
                                Font = current_font,
                                LinkID = 0,
                                Color = current_charcolor
                            };

                            ptr.Data.Add(mfd1);
                            readWidth += (data[0] + data[2] + 1);

                            if ((data[1] + data[3]) > ptr.MaxHeight)
                                ptr.MaxHeight = (data[1] + data[3]);

                            charCount++;

                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;

                        charcolor = lastspace_charcolor;
                        current_charcolor = lastspace_charcolor;
                        si = str[i];

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;
                        ptr.Data.Resize(ptr.CharCount);

                        if (isFixed || isCropped)
                            break;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;

                        ptr.Align = current_align;
                        ptr.CharStart = i;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (current_flags & UOFONT_INDENTION) != 0)
                            indetionOffset = 14;
                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData()
                {
                    Item = si,
                    Flags = current_flags,
                    Font = current_font,
                    LinkID = 0,
                    Color = current_charcolor
                };
                ptr.Data.Add(mfd);

                if (si == ' ')
                {
                    readWidth += UNICODE_SPACE_WIDTH;
                    if (ptr.MaxHeight <= 0)
                        ptr.MaxHeight = 5;
                }
                else
                {
                    readWidth += (data[0] + data[2] + 1);
                    if ((data[1] + data[3]) > ptr.MaxHeight)
                        ptr.MaxHeight = (data[1] + data[3]);

                }

                charCount++;             
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;

            if (readWidth <= 0 && len > 0 && (str[len - 1] == '\n' || str[len - 1] == '\r'))
            {
                ptr.Width = 1;
                ptr.MaxHeight = 14;
            }

            return info;
        }

        private static unsafe (uint[], int, int, int, List<WebLinkRect>) GeneratePixelsUnicode(in byte font, in string str, in ushort color, in byte cell, int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            uint[] pData;
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero)
                return (null, 0, 0, 0, null);

            int len = str.Length;
            if (len <= 0)
                return(null, 0, 0, 0, null);

            int oldWidth = width;
            if (width <= 0)
            {
                width = GetWidthUnicode(font, str);
                if (width <= 0)
                    return (null, 0, 0, 0, null);
            }

            MultilinesFontInfo info = GetInfoUnicode(font, str, len, align, flags, width);
            if (info == null)
                return (null, 0, 0, 0, null);

            if (_useHTML && (_leftMargin > 0 || _rightMargin > 0))
            {
                while (info != null)
                {
                    MultilinesFontInfo ptr1 = info.Next;
                    info.Data.Clear();
                    info = null;
                    info = ptr1;
                }

                int newWidth = width - (_leftMargin + _rightMargin);

                if (newWidth < 10)
                    newWidth = 10;
                info = GetInfoUnicode(font, str, len, align, flags, newWidth);
                if (info == null)
                    return (null, 0, 0, 0, null);
            }

            if (oldWidth <= 0 && RecalculateWidthByInfo)
            {
                MultilinesFontInfo ptr1 = info;
                width = 0;
                while (ptr1 != null)
                {
                    if (ptr1.Width > width)
                        width = ptr1.Width;
                    ptr1 = ptr1.Next;
                }
            }

            width += 4;

            int height = GetHeightUnicode(info);
            if (height <= 0)
            {
                while (info != null)
                {
                    MultilinesFontInfo ptr1 = info;
                    info = info.Next;
                    ptr1.Data.Clear();
                    ptr1 = null;
                }
                return (null, 0, 0, 0, null);
            }

            height += _topMargin + _bottomMargin + 4;
            int blocksize = height * width;
            pData = new uint[blocksize];

            uint* table = (uint*)_unicodeFontAddress[font];
            int lineOffsY = 1 + _topMargin;

            MultilinesFontInfo ptr = info;

            uint datacolor = 0;

            if (color == 0xFFFF)
                datacolor = /*0xFFFFFFFE;*/  Hues.RgbaToArgb(0xFFFFFFFE);
            else
                datacolor = /*Hues.GetPolygoneColor(cell, color) << 8 | 0xFF;*/  Hues.RgbaToArgb(Hues.GetPolygoneColor(cell, color) << 8 | 0xFF);

            bool isItalic = (flags & UOFONT_ITALIC) != 0;
            bool isSolid = (flags & UOFONT_SOLID) != 0;
            bool isBlackBorder = (flags & UOFONT_BLACK_BORDER) != 0;
            bool isUnderline = (flags & UOFONT_UNDERLINE) != 0;
            uint blackColor = Hues.RgbaToArgb(0x010101FF);

            bool isLink = false;
            int linkStartX = 0;
            int linkStartY = 0;

            int linesCount = 0;
            List<WebLinkRect> links = new List<WebLinkRect>();

            while (ptr != null)
            {
                info = ptr;
                linesCount++;

                int w = _leftMargin;

                if (ptr.Align == TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    w += (((width - 10) - ptr.Width) / 2);
                    if (w < 0)
                        w = 0;
                }
                else if (ptr.Align == TEXT_ALIGN_TYPE.TS_RIGHT)
                {
                    w += ((width - 10) - ptr.Width);
                    if (w < 0)
                        w = 0;
                }
                else if(ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                {
                    w += ptr.IndentionOffset;
                }

                ushort oldLink = 0;

                int dataSize = ptr.Data.Count;

                for (int i = 0; i < dataSize; i++)
                {
                    MultilinesFontData data = ptr.Data[i];
                    char si = data.Item;

                    table = (uint*)_unicodeFontAddress[data.Font];

                    if (!isLink)
                    {
                        oldLink = data.LinkID;
                        if (oldLink > 0)
                        {
                            isLink = true;
                            linkStartX = w;
                            linkStartY = lineOffsY + 3;
                        }
                    }
                    else if (data.LinkID <= 0 || i + 1 == dataSize)
                    {
                        isLink = false;
                        int linkHeight = lineOffsY - linkStartY;
                        if (linkHeight < 14)
                            linkHeight = 14;

                        int ofsX = 0;

                        if (si == ' ')
                            ofsX = UNICODE_SPACE_WIDTH;
                        else if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ')
                        {

                        }
                        else
                        {
                            byte* xData = (byte*)(((IntPtr)table) + (int)table[si]);
                            ofsX = (sbyte)xData[2];
                        }

                        WebLinkRect wlr = new WebLinkRect()
                        {
                            LinkID = oldLink,
                            StartX = linkStartX,
                            StartY = linkStartY,
                            EndX = w - ofsX,
                            EndY = linkHeight
                        };

                        links.Add(wlr);
                        oldLink = 0;
                    }

                    if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ')
                        continue;

                    byte* ddata = (byte*)(((IntPtr)table) + (int)table[si]);
                    int offsX = 0;
                    int offsY = 0;
                    int dw = 0;
                    int dh = 0;

                    if (si == ' ')
                    {
                        offsX = 0;
                        dw = UNICODE_SPACE_WIDTH;
                    }
                    else
                    {
                        offsX = ddata[0] + 1;
                        offsY = ddata[1];
                        dw = ddata[2];
                        dh = ddata[3];

                        ddata = (byte*)((IntPtr)ddata + 4);
                    }

                    int tmpW = w;
                    uint charcolor = datacolor;
                    bool isBlackPixel = (((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 &&
                                        ((charcolor >> 8) & 0xFF) <= 8);
                    if (si != ' ')
                    {
                        if (_useHTML && i < ptr.Data.Count)
                        {
                            isItalic = (data.Flags & UOFONT_ITALIC) != 0;
                            isSolid = (data.Flags & UOFONT_SOLID) != 0;
                            isBlackBorder = (data.Flags & UOFONT_BLACK_BORDER) != 0;
                            isUnderline = (data.Flags & UOFONT_UNDERLINE) != 0;

                            if (data.Color != 0xFFFFFFFF)
                            {
                                charcolor = data.Color;
                                isBlackPixel =
                                           (((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 &&
                                           ((charcolor >> 8) & 0xFF) <= 8);
                            }
                        }

                        int scanlineCount = ((dw - 1) / 8) + 1;
                        for (int y = 0; y < dh; y++)
                        {
                            int testY = offsY + lineOffsY + y;
                            if (testY >= height)
                                break;

                            byte* scanlines = ddata;
                            //ddata += scanlineCount;

                            ddata = (byte*)((IntPtr)ddata + scanlineCount);

                            int italicOffset = 0;
                            if (isItalic)
                                italicOffset = (int)((dh - y) / ITALIC_FONT_KOEFFICIENT);

                            int testX = w + offsX + italicOffset + (isSolid ? 1 : 0);

                            for (int c = 0; c < scanlineCount; c++)
                            {
                                for (int j = 0; j < 8; j++)
                                {
                                    int x = (c * 8) + j;
                                    if (x >= dw)
                                        break;

                                    int nowX = testX + x;
                                    if (nowX >= width)
                                        break;

                                    byte cl = (byte)(scanlines[c] & (1 << (7 - j)));
                                    int block = (testY * width) + nowX;

                                    if (cl > 0)
                                        pData[block] = charcolor;
                                }
                            }
                        }

                        if (isSolid)
                        {
                            uint solidColor = Hues.RgbaToArgb( blackColor );

                            if (solidColor == charcolor)
                                solidColor++;

                            int minXOk = ((w + offsX) > 0) ? -1 : 0;
                            int maxXOk = ((w + offsX + dw) < width) ? 1 : 0;

                            maxXOk += dw;

                            for (int cy = 0; cy < dh; cy++)
                            {
                                int testY = offsY + lineOffsY + cy;

                                if (testY >= height)
                                    break;

                                int italicOffset = 0;
                                if (isItalic && cy < dh)
                                    italicOffset = (int)((dh - (int)cy) / ITALIC_FONT_KOEFFICIENT);

                                for (int cx = minXOk; cx < maxXOk; cx++)
                                {
                                    int testX = (int)cx + w + offsX + italicOffset;

                                    if (testX >= width)
                                        break;

                                    int block = (testY * width) + testX;

                                    if (pData[block] <= 0 && pData[block] != solidColor)
                                    {
                                        int endX = (cx < dw) ? 2 : 1;

                                        if (endX == 2 && (testX + 1) >= width)
                                            endX--;

                                        for (int x = 0; x < endX; x++)
                                        {
                                            int nowX = testX + x;

                                            int testBlock = (testY * width) + nowX;

                                            if (pData[testBlock] > 0 && pData[testBlock] != solidColor)
                                            {
                                                pData[block] = solidColor;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            for (int cy = 0; cy < dh;  cy++)
                            {
                                int testY = offsY + lineOffsY + cy;

                                if (testY >= height)
                                    break;

                                int italicOffset = 0;
                                if (isItalic)
                                    italicOffset = (int)((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                for (int cx = 0; cx < dw; cx++)
                                {
                                    int testX = cx + w + offsX + italicOffset;

                                    if (testX >= width)
                                        break;

                                    int block = (testY * width) + testX;

                                    if (pData[block] == solidColor)
                                        pData[block] = charcolor;
                                }
                            }
                        }


                        if (isBlackBorder && !isBlackPixel)
                        {
                            int minXOk = (w + offsX > 0) ? -1 : 0;
                            int minYOk = (offsY + lineOffsY > 0) ? -1 : 0;
                            int maxXOk = (w + offsX + dw < width) ? 1 : 0;
                            int maxYOk = (offsY + lineOffsY + dh < height) ? 1 : 0;

                            maxXOk += dw;
                            maxYOk += dh;

                            for (int cy = minYOk; cy < maxYOk; cy++)
                            {
                                int testY = offsY + lineOffsY + cy;

                                if (testY >= height)
                                    break;

                                int italicOffset = 0;
                                if (isItalic && cy >= 0 && cy < dh)
                                    italicOffset = (int)((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                for (int cx = minXOk; cx < maxXOk; cx++)
                                {
                                    int testX = cx + w + offsX + italicOffset;

                                    if (testX >= width)
                                        break;

                                    int block = (testY * width) + testX;

                                    if (pData[block] <= 0 && pData[block] != blackColor)
                                    {
                                        int startX = (cx > 0) ? -1 : 0;
                                        int startY = (cy > 0) ? -1 : 0;
                                        int endX = (cx < dw - 1) ? 2 : 1;
                                        int endY = (cy < dh - 1) ? 2 : 1;

                                        if (endX == 2 && (testX + 1) >= width)
                                            endX--;

                                        bool passed = false;

                                        for (int x = startX; x < endX; x++)
                                        {
                                            int nowX = testX + x;
                                            for (int y = startY; y < endY; y++)
                                            {
                                                int testBlock = ((testY + y) * width) + nowX;
                                                if (pData[testBlock] > 0 && pData[testBlock] != blackColor)
                                                {
                                                    pData[block] = blackColor;

                                                    passed = true;

                                                    break;
                                                }
                                            }

                                            if (passed)
                                                break;
                                        }
                                    }                             
                                }
                            }
                        }

                        w += (dw + offsX + (isSolid ? 1 : 0));
                    }
                    else if (si == ' ')
                    {
                        w += UNICODE_SPACE_WIDTH;

                        if (_useHTML)
                        {
                            isUnderline = (data.Flags & UOFONT_UNDERLINE) != 0;
                            if (data.Color != 0xFFFFFFFF)
                            {
                                charcolor = data.Color;
                                isBlackPixel =
                                    (((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 &&
                                     ((charcolor >> 8) & 0xFF) <= 8);
                            }
                        }
                    }

                    if (isUnderline)
                    {
                        int minXOk = ((tmpW + offsX) > 0) ? -1 : 0;
                        int maxXOk = ((w + offsX + dw) < width) ? 1 : 0;

                        byte* aData = (byte*)(((IntPtr)table) + (int)table[(byte)'a']);

                        int testY = lineOffsY + aData[1] + aData[3];

                        if (testY >= height)
                            break;

                        for (int cx = minXOk; cx < dw + maxXOk; cx++)
                        {
                            int testX = (cx + tmpW + offsX + (isSolid ? 1 : 0));

                            if (testX >= width)
                                break;

                            int block = (testY * width) + testX;

                            pData[block] = charcolor;
                        }
                    }
                }

                lineOffsY += ptr.MaxHeight;
                ptr = ptr.Next;
                info.Data.Clear();
                info = null;
            }

            if (_useHTML && _HTMLBackgroundCanBeColored && _backgroundColor > 0)
            {
                _backgroundColor |= 0xFF;

                for (int y = 0; y < height; y++)
                {
                    int yPos = (y * width);
                    for (int x = 0; x < width; x++)
                    {
                        if (pData[yPos + x] <= 0)
                            pData[yPos + x] = Hues.RgbaToArgb(_backgroundColor);
                    }
                }
            }

            return (pData, width, height, linesCount, links);
        }

        private static unsafe MultilinesFontInfo GetInfoHTML(in byte font, in string str, int len, in TEXT_ALIGN_TYPE align, in ushort flags, in int width)
        {
            HTMLChar[] htmlData = GetHTMLData(font, str, ref len, align, flags);

            if (htmlData.Length <= 0)
                return null;

            MultilinesFontInfo info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;

            MultilinesFontInfo ptr = info;
            int indentionOffset = 0;

            ptr.IndentionOffset = indentionOffset;

            int charCount = 0;
            int lastSpace = 0;
            int readWidth = 0;

            bool isFixed = ((flags & UOFONT_FIXED) != 0);
            bool isCropped = ((flags & UOFONT_CROPPED) != 0);

            if (len > 0)
                ptr.Align = htmlData[0].Align;

            for (int i = 0; i < len; i++)
            {
                char si = htmlData[i].Char;
                uint* table = (uint*)_unicodeFontAddress[htmlData[i].Font];

                if ((byte)si == 0x000D || si == '\n')
                {
                    if ((byte)si == 0x000D || isFixed || isCropped)
                        si = (char)0;
                    else si = '\n';
                }

                if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n')
                    continue;

                byte* data = (byte*)(((IntPtr)table) + (int)table[(byte)si]);

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                }

                int solidWidth = htmlData[i].Flags & UOFONT_SOLID;
                if (ptr.Width + readWidth + ((byte)data[0] + (byte)data[2]) + solidWidth >width || si == '\n')
                {
                    if (lastSpace == ptr.CharStart && lastSpace <= 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        lastSpace = i;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                        ptr.Data.Resize(ptr.CharCount);

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = htmlData[i].Align;
                        ptr.CharStart = i + 1;

                        readWidth = 0;
                        charCount = 0;
                        indentionOffset = 0;

                        ptr.IndentionOffset = indentionOffset;
                        continue;
                    }
                    else if (lastSpace + 1 == ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;

                        ptr.Align = htmlData[i].Align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (htmlData[i].Flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            MultilinesFontData mfd1 = new MultilinesFontData()
                            {
                                Item = si,
                                Flags = htmlData[i].Flags,
                                Font = htmlData[i].Font,
                                LinkID = htmlData[i].LinkID,
                                Color = htmlData[i].Color
                            };

                            ptr.Data.Add(mfd1); ;
                            readWidth += ((sbyte)data[0] + (sbyte)data[2] + 1);
                            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                            charCount++;

                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }


                        i = lastSpace + 1;

                        si = htmlData[i].Char;
                        solidWidth = htmlData[i].Flags & UOFONT_SOLID;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                        ptr.Data.Resize(ptr.CharCount);
                        charCount = 0;

                        if (isFixed || isCropped)
                            break;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = htmlData[i].Align;
                        ptr.CharCount = i;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (htmlData[i].Flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;
                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData()
                {
                    Item = si,
                    Flags = htmlData[i].Flags,
                    Font = htmlData[i].Font,
                    LinkID = htmlData[i].LinkID,
                    Color = htmlData[i].Color
                };
                ptr.Data.Add(mfd);

                if (si == ' ')
                    readWidth += UNICODE_SPACE_WIDTH;
                else
                    readWidth += (data[0] + data[2] + 1) + solidWidth;

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;
            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

            return info;
        }

        private static HTMLChar[] GetHTMLData(in byte font, in string str, ref int len, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            HTMLChar[] data = new HTMLChar[0];

            if (len < 1)
                return data;

            data = new HTMLChar[len];

            int newlen = 0;

            HTMLDataInfo info = new HTMLDataInfo()
            {
                Tag = HTML_TAG_TYPE.HTT_NONE,
                Align = align,
                Flags = flags,
                Font = font,
                Color = _HTMLColor,
                Link = 0
            };
            List<HTMLDataInfo> stack = new List<HTMLDataInfo>();
            stack.Add(info);

            HTMLDataInfo currentInfo = info;

            for (int i = 0; i < len; i++)
            {
                char si = str[i];

                if (si == '<')
                {
                    bool endTag = false;
                    HTMLDataInfo newInfo = new HTMLDataInfo()
                    {
                        Tag = HTML_TAG_TYPE.HTT_NONE,
                        Align = TEXT_ALIGN_TYPE.TS_LEFT,
                        Flags = 0,
                        Font = 0xFF,
                        Color = 0,
                        Link = 0
                    };

                    HTML_TAG_TYPE tag = ParseHTMLTag(str, len, ref i, ref endTag, newInfo);

                    if (tag == HTML_TAG_TYPE.HTT_NONE)
                        continue;

                    if (!endTag)
                    {
                        if (newInfo.Font == 0xFF)
                            newInfo.Font = stack.LastOrDefault().Font;

                        if (tag != HTML_TAG_TYPE.HTT_BODY)
                            stack.Add(newInfo);
                        else
                        {
                            stack.Clear();
                            newlen = 0;

                            if (newInfo.Color > 0)
                                info.Color = newInfo.Color;
                            stack.Add(info);
                        }
                    }
                    else if (stack.Count > 1)
                    {
                        int index = -1;
                        for( int j = stack.Count -1; j > 1; j--)
                        {
                            if (stack[j].Tag == tag)
                            {
                                stack.RemoveAt(j); // MAYBE ERROR?
                                break;
                            }
                        }
                    }

                    currentInfo = GetCurrentHTMLInfo(stack);

                    switch (tag)
                    {
                        case HTML_TAG_TYPE.HTT_LEFT:
                        case HTML_TAG_TYPE.HTT_CENTER:
                        case HTML_TAG_TYPE.HTT_RIGHT:
                            if (newlen > 0) endTag = true;
                            goto case HTML_TAG_TYPE.HTT_P;
                        case HTML_TAG_TYPE.HTT_P:
                            if (endTag)
                                si = '\n';
                            else
                                si = (char)0;
                            break;
                        case HTML_TAG_TYPE.HTT_BR:
                        case HTML_TAG_TYPE.HTT_BQ:
                            si = '\n';
                            break;
                        default: si = (char)0; break;
                    }
                }

                if ((byte)si > 0)
                {
                    data[newlen].Char = si;
                    data[newlen].Font = currentInfo.Font;
                    data[newlen].Align = currentInfo.Align;
                    data[newlen].Flags = currentInfo.Flags;
                    data[newlen].Color = currentInfo.Color;
                    data[newlen].LinkID = currentInfo.Link;
                    newlen++;
                }
            }

            Array.Resize(ref data, newlen);
            len = newlen;
            return data;
        }

        private static HTMLDataInfo GetCurrentHTMLInfo(in List<HTMLDataInfo> list)
        {
            HTMLDataInfo info = new HTMLDataInfo()
            {
                Tag = HTML_TAG_TYPE.HTT_NONE,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                Flags = 0,
                Font = 0xFF,
                Color = 0,
                Link = 0
            };

            for (int i = 0; i < list.Count; i++)
            {
                var current = list[i];

                switch (current.Tag)
                {
                    case HTML_TAG_TYPE.HTT_NONE:
                        info = current;
                        break;
                    case HTML_TAG_TYPE.HTT_B:
                    case HTML_TAG_TYPE.HTT_I:
                    case HTML_TAG_TYPE.HTT_U:
                    case HTML_TAG_TYPE.HTT_P:
                        info.Flags |= current.Flags;
                        break;
                    case HTML_TAG_TYPE.HTT_A:
                        info.Flags |= current.Flags;
                        info.Color = current.Color;
                        info.Link = current.Link;
                        break;
                    case HTML_TAG_TYPE.HTT_BIG:
                    case HTML_TAG_TYPE.HTT_SMALL:
                        if (current.Font == 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                            info.Font = current.Font;
                        break;
                    case HTML_TAG_TYPE.HTT_BASEFONT:
                        if (current.Font != 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                            info.Font = current.Font;

                        if (current.Color != 0)
                            info.Color = current.Color;
                        break;
                    case HTML_TAG_TYPE.HTT_H1:
                    case HTML_TAG_TYPE.HTT_H2:
                    case HTML_TAG_TYPE.HTT_H4:
                    case HTML_TAG_TYPE.HTT_H5:
                        info.Flags |= current.Flags;
                        goto case HTML_TAG_TYPE.HTT_H3;
                    case HTML_TAG_TYPE.HTT_H3:
                    case HTML_TAG_TYPE.HTT_H6:
                        if (current.Font != 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                            info.Font = current.Font;
                        break;
                    case HTML_TAG_TYPE.HTT_BQ:
                        info.Color = current.Color;
                        info.Flags |= current.Flags;
                        break;
                    case HTML_TAG_TYPE.HTT_LEFT:
                    case HTML_TAG_TYPE.HTT_CENTER:
                    case HTML_TAG_TYPE.HTT_RIGHT:
                        info.Align = current.Align;
                        break;
                    case HTML_TAG_TYPE.HTT_DIV:
                        info.Align = current.Align;
                        break;
                }
            }
            return info;
        }

        private static HTML_TAG_TYPE ParseHTMLTag(in string str, in int len, ref int i, ref bool endTag, HTMLDataInfo info)        
        {
            HTML_TAG_TYPE tag = HTML_TAG_TYPE.HTT_NONE;
            i++;

            if (i < len && str[i] == '/')
            {
                endTag = true;
                i++;
            }

            while (str[i] == ' ' && i < len)
                i++;

            int j = i;
            for (;i < len; i++)
            {
                if (str[i] == ' ' || str[i] == '>')
                    break;
            }

            if (j != i && i < len)
            {
                int cmdLen = i - j;
                string cmd = str.Substring(j, cmdLen);

                cmd = cmd.ToLower();
                j = i;

                while (str[i] != '>' && i < len)
                    i++;

                switch (cmd)
                {
                    case "b": tag = HTML_TAG_TYPE.HTT_B; break;
                    case "i": tag = HTML_TAG_TYPE.HTT_I; break;
                    case "a": tag = HTML_TAG_TYPE.HTT_A; break;
                    case "u": tag = HTML_TAG_TYPE.HTT_U; break;
                    case "p": tag = HTML_TAG_TYPE.HTT_P; break;
                    case "big": tag = HTML_TAG_TYPE.HTT_BIG; break;
                    case "small": tag = HTML_TAG_TYPE.HTT_SMALL; break;
                    case "body": tag = HTML_TAG_TYPE.HTT_BODY; break;
                    case "basefont": tag = HTML_TAG_TYPE.HTT_BASEFONT; break;
                    case "h1": tag = HTML_TAG_TYPE.HTT_H1; break;
                    case "h2": tag = HTML_TAG_TYPE.HTT_H2; break;
                    case "h3": tag = HTML_TAG_TYPE.HTT_H3; break;
                    case "h4": tag = HTML_TAG_TYPE.HTT_H4; break;
                    case "h5": tag = HTML_TAG_TYPE.HTT_H5; break;
                    case "h6": tag = HTML_TAG_TYPE.HTT_H6; break;
                    case "br": tag = HTML_TAG_TYPE.HTT_BR; break;
                    case "bq": tag = HTML_TAG_TYPE.HTT_BQ; break;
                    case "left": tag = HTML_TAG_TYPE.HTT_LEFT; break;
                    case "center": tag = HTML_TAG_TYPE.HTT_CENTER; break;
                    case "right": tag = HTML_TAG_TYPE.HTT_RIGHT; break;
                    case "div": tag = HTML_TAG_TYPE.HTT_DIV; break;
                }


                if (!endTag)
                {
                    info = GetHTMLInfoFromTag(tag);

                    if (i < len && j != i)
                    {
                        switch (tag)
                        {
                            case HTML_TAG_TYPE.HTT_BODY:
                            case HTML_TAG_TYPE.HTT_BASEFONT:
                            case HTML_TAG_TYPE.HTT_A:
                            case HTML_TAG_TYPE.HTT_DIV:

                                string content = "";
                                cmdLen = i - j;
                                content = content.Substring(j, cmdLen);

                                if (content.Length > 0)
                                    GetHTMLInfoFromContent(ref info, content);

                                break;
                        }
                    }
                }
            }

            return tag;
        }

        private static void GetHTMLInfoFromContent(ref HTMLDataInfo info, in string content)
        {
            string[] strings = content.Split(new char[] { ' ', '=',  '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int size = strings.Length;

            for (int i = 0; i < size; i+= 2)
            {
                if (i + 1 >= size)
                    break;

                string str = strings[i].ToLower();
                string value = strings[i + 1];
                TrimHTMLString(ref value);

                if (value.Length <= 0)
                    continue;

                switch (info.Tag)
                {
                    case HTML_TAG_TYPE.HTT_BODY:
                        
                        switch (str)
                        {
                            case "text":
                                info.Color = GetHTMLColorFromText(ref value);
                                break;
                            case "bgcolor":
                                if (_HTMLBackgroundCanBeColored)
                                    _backgroundColor = GetHTMLColorFromText(ref value);
                                break;
                            case "link":
                                _webLinkColor = GetHTMLColorFromText(ref value);
                                break;
                            case "vlink":
                                _visitedWebLinkColor = GetHTMLColorFromText(ref value);
                                break;
                            case "leftmargin":
                                _leftMargin = int.Parse(value);
                                break;
                            case "topmargin":
                                _topMargin = int.Parse(value);
                                break;
                            case "rightmargin":
                                _rightMargin = int.Parse(value);
                                break;
                            case "bottommargin":
                                _bottomMargin = int.Parse(value);
                                break;
                        }

                        break;
                    case HTML_TAG_TYPE.HTT_BASEFONT:
                        if (str == "color")
                            info.Color = GetHTMLColorFromText(ref value);
                        else if (str == "size")
                        {
                            byte font = byte.Parse(value);
                            if (font == 0 || font == 4)
                                info.Font = 1;
                            else if (font < 4)
                                info.Font = 2;
                            else
                                info.Font = 0;
                        }
                        break;
                    case HTML_TAG_TYPE.HTT_A:
                        if (str == "href")
                        {
                            info.Flags = UOFONT_UNDERLINE;
                            info.Color = _webLinkColor;
                            info.Link = GetWebLinkID(value, ref info.Color);
                        }
                        break;
                    case HTML_TAG_TYPE.HTT_DIV:
                        if (str == "align")
                        {
                            str = value.ToLower();
                            switch (str)
                            {
                                case "left":
                                    info.Align = TEXT_ALIGN_TYPE.TS_LEFT;
                                    break;
                                case "center":
                                    info.Align = TEXT_ALIGN_TYPE.TS_CENTER;
                                    break;
                                case "right":
                                    info.Align = TEXT_ALIGN_TYPE.TS_RIGHT;
                                    break;
                            }                     
                        }
                        break;
                }
            }
        }

        private static ushort GetWebLinkID(in string link, ref uint color)
        {
            ushort linkID = 0;
            KeyValuePair<ushort, WebLink>? l = null;

            foreach (KeyValuePair<ushort, WebLink> ll in _webLinks)
            {
                if (ll.Value.Link == link)
                {
                    l = ll;
                    break;
                }
            }

            if (l == null || !l.HasValue)
            {
                linkID = (ushort)(_webLinks.Count + 1);
                _webLinks[linkID] = new WebLink() { IsVisited = false, Link = link };
            }
            else
            {
                if (l.Value.Value.IsVisited)
                    color = _visitedWebLinkColor;
                linkID = l.Value.Key;
            }

            return linkID;
        }

        private static unsafe uint GetHTMLColorFromText(ref string str)
        {
            uint color = 0;

            if (str.Length > 1)
            {
                if (str[0] == '#')
                {
                    color = str.Substring(1).StartsWith("0x") ? Convert.ToUInt32(str.Substring(3), 16) : Convert.ToUInt32(str.Substring(1), 10);

                    byte* clrBuf = (byte*)color;
                    color = (uint)((clrBuf[0] << 24) | (clrBuf[1] << 16) | (clrBuf[2] << 8) | 0xFF);
                }
                else
                {
                    str = str.ToLower();
                    
                    switch (str)
                    {
                        case "red": color = 0x0000FFFF; break;
                        case "cyan": color = 0xFFFF00FF; break;
                        case "blue": color = 0xFF0000FF; break;
                        case "darkblue": color = 0xA00000FF; break;
                        case "lightblue": color = 0xE6D8ADFF; break;
                        case "purple": color = 0x800080FF; break;
                        case "yellow": color = 0x00FFFFFF; break;
                        case "lime": color = 0x00FF00FF; break;
                        case "magenta": color = 0xFF00FFFF; break;
                        case "white": color = 0xFFFEFEFF; break;
                        case "silver": color = 0xC0C0C0FF; break;
                        case "gray": case "grey": color = 0x808080FF; break;
                        case "blackv": color = 0x010101FF; break;
                        case "orange": color = 0x00A5FFFF; break;
                        case "brown": color = 0x2A2AA5FF; break;
                        case "maroon": color = 0x000080FF; break;
                        case "green": color = 0x008000FF; break;
                        case "olive": color = 0x008080FF; break;
                    }
                }
            }

            return color;
        }

        private static void TrimHTMLString(ref string str)
        {
            if (str.Length >= 2 && str[0] == '"' && str[str.Length - 1] == '"')
                str = str.Remove(str.Length - 1).Remove(0);
        }

        private static HTMLDataInfo GetHTMLInfoFromTag(in HTML_TAG_TYPE tag)
        {
            HTMLDataInfo info = new HTMLDataInfo()
            {
                Tag = tag,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                Flags = 0,
                Font = 0xFF,
                Color = 0,
                Link = 0
            };

            switch (tag)
            {
                case HTML_TAG_TYPE.HTT_B: info.Flags = UOFONT_SOLID; break;
                case HTML_TAG_TYPE.HTT_I: info.Flags = UOFONT_ITALIC; break;
                case HTML_TAG_TYPE.HTT_U: info.Flags = UOFONT_UNDERLINE; break;
                case HTML_TAG_TYPE.HTT_P: info.Flags = UOFONT_INDENTION; break;
                case HTML_TAG_TYPE.HTT_BIG: info.Font = 0; break;
                case HTML_TAG_TYPE.HTT_SMALL: info.Font = 2; break;
                case HTML_TAG_TYPE.HTT_H1: info.Flags = UOFONT_SOLID | UOFONT_UNDERLINE; info.Font = 0; break;
                case HTML_TAG_TYPE.HTT_H2: info.Flags = UOFONT_SOLID; info.Font = 0; break;
                case HTML_TAG_TYPE.HTT_H3: info.Font = 0; break;
                case HTML_TAG_TYPE.HTT_H4: info.Flags = UOFONT_SOLID; info.Font = 2; break;
                case HTML_TAG_TYPE.HTT_H5: info.Flags = UOFONT_ITALIC; info.Font = 2; break;
                case HTML_TAG_TYPE.HTT_H6: info.Font = 2; break;
                case HTML_TAG_TYPE.HTT_BQ: info.Flags = UOFONT_BQ; info.Color = 0x008000FF; break;
                case HTML_TAG_TYPE.HTT_LEFT: info.Align = TEXT_ALIGN_TYPE.TS_LEFT; break;
                case HTML_TAG_TYPE.HTT_CENTER: info.Align = TEXT_ALIGN_TYPE.TS_CENTER; break;
                case HTML_TAG_TYPE.HTT_RIGHT: info.Align = TEXT_ALIGN_TYPE.TS_RIGHT; break;
            }

            return info;
        }

        private static int GetHeightUnicode(MultilinesFontInfo info)
        {
            int textHeight = 0;
            for(; info != null; info = info.Next)
            {
                if (_useHTML)
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                else
                    textHeight += info.MaxHeight;
            }

            return textHeight;
        }


    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FontHeader
    {
        public byte Width, Height, Unknown;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FontCharacter
    {
        public byte Width, Height, Unknown;
    }

    public struct FontCharacterData
    {
        public byte Width, Height;
        public List<ushort> Data;
    }

    public struct FontData
    {
        public byte Header;
        // 224
        public FontCharacterData[] Chars;
    }

    public class MultilinesFontInfo
    {
        public int Width;
        public int IndentionOffset;
        public int MaxHeight;
        public int CharStart;
        public int CharCount;
        public TEXT_ALIGN_TYPE Align;
        public List<MultilinesFontData> Data = new List<MultilinesFontData>();
        public MultilinesFontInfo Next;

        public void Reset()
        {
            Width = 0;
            IndentionOffset = 0;
            MaxHeight = 0;
            CharStart = 0;
            CharCount = 0;
            Align = TEXT_ALIGN_TYPE.TS_LEFT;
            Next = null;
        }
    }

    public class MultilinesFontData
    {
        public char Item;
        public ushort Flags;
        public byte Font;
        public ushort LinkID;
        public uint Color;

        public MultilinesFontData Next;
    }

    public struct WebLinkRect 
    {
        public ushort LinkID;
        public int StartX, StartY, EndX, EndY;
    }

    public struct WebLink
    {
        public bool IsVisited;
        public string Link;
    }

    public struct HTMLChar
    {
        public char Char;
        public byte Font;
        public TEXT_ALIGN_TYPE Align;
        public ushort Flags;
        public uint Color;
        public ushort LinkID;
    }

    public struct HTMLDataInfo
    {
        public HTML_TAG_TYPE Tag;
        public TEXT_ALIGN_TYPE Align;
        public ushort Flags;
        public byte Font;
        public uint Color;
        public ushort Link;
    }
}