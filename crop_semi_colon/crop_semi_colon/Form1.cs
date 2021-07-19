using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HalconDotNet;

namespace crop_semi_colon
{
    public partial class Form1 : Form
    {
        HObject _image;
        bool _flg;
        bool _flg1;
        double _x;
        double _y;
        HObject _rectangle;
        HObject _regionCharacters;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            hSmartWindowControl1.MouseWheel += hSmartWindowControl1.HSmartWindowControl_MouseWheel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            HOperatorSet.ReadImage(out _image, textBox1.Text);
            hSmartWindowControl1.HalconWindow.DispObj(_image);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _flg = true;
            hSmartWindowControl1.HMoveContent = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {

            HOperatorSet.ReduceDomain(_image, _rectangle, out HObject imageReduced);
            HTuple widthAve = new HTuple(50, 1, 150);
            HTuple heightAve = new HTuple(50, 1, 150);
            HTuple contrast = 150;
            HOperatorSet.SegmentCharacters(imageReduced, imageReduced, out HObject imageForeground,
                out HObject regionForeground, "local_contrast_best", "false", "false", "medium",
                widthAve, heightAve, 0, contrast, out HTuple usedThreshold);
            HTuple connectFragments = "true";
            HTuple flagmentDistance = "medium";
            //HTuple flagmentDistance = "wide";
            HTuple charWidth = widthAve;
            HTuple charHeight = heightAve;
            HOperatorSet.SelectCharacters(regionForeground, out _regionCharacters,
                "false", "medium", charWidth, charHeight, "true", "false", "variable_width", "false", flagmentDistance,
                connectFragments, 0, "completion");

            hSmartWindowControl1.HalconWindow.DispObj(_image);
            hSmartWindowControl1.HalconWindow.SetColored(12);
            hSmartWindowControl1.HalconWindow.DispObj(_regionCharacters);

        }



        private void hSmartWindowControl1_HMouseDown(object sender, HMouseEventArgs e)
        {
            _flg1 = true;
            _x = e.X;
            _y = e.Y;
        }

        private void hSmartWindowControl1_HMouseMove(object sender, HMouseEventArgs e)
        {
            if (_image == null)
            {
                return;
            }
            if (!_flg)
            {
                return;
            }
            if (!_flg1)
            {
                return;
            }
            HOperatorSet.GenRectangle1(out _rectangle, _y, _x, e.Y, e.X);
            hSmartWindowControl1.HalconWindow.DispObj(_image);
            hSmartWindowControl1.HalconWindow.SetDraw("margin");
            hSmartWindowControl1.HalconWindow.SetColor("red");
            hSmartWindowControl1.HalconWindow.DispObj(_rectangle);
        }

        private void hSmartWindowControl1_HMouseUp(object sender, HMouseEventArgs e)
        {
            _flg = false;
            _flg1 = false;
            hSmartWindowControl1.HMoveContent = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // : ; 対策で領域を縦に拡張する
            HOperatorSet.SortRegion(_regionCharacters, out _regionCharacters, "first_point", "true", "column");
            HOperatorSet.CountObj(_regionCharacters, out HTuple number);
            HTuple oldColumns = new HTuple();
            HOperatorSet.GenEmptyObj(out HObject oldRegion);
            HTuple oldIndex = 0;
            HOperatorSet.GenEmptyObj(out HObject newRegion);
            for (int i = 1; i <= number; i++)
            {
                HOperatorSet.SelectObj(_regionCharacters, out HObject objectSelected, i);
                HOperatorSet.GetRegionPoints(objectSelected, out HTuple __, out HTuple columns);
                if (i == 1)
                {
                    oldColumns = columns;
                    HOperatorSet.CopyObj(objectSelected, out oldRegion, 1, 1);
                    oldIndex = i;
                    continue;
                }
                bool flg = false;
                for (int j = 0; j < oldColumns.Length; j++)
                {
                    for (int k = 0; k < columns.Length; k++)
                    {
                        HOperatorSet.TupleEqual(oldColumns[j], columns[k], out HTuple equal);
                        if (equal)
                        {
                            HOperatorSet.Union2(oldRegion, objectSelected, out HObject regionUnion);
                            HOperatorSet.ConcatObj(newRegion, regionUnion, out newRegion);
                            i++;
                            flg = true;
                            if (i <= number)
                            {
                                HOperatorSet.SelectObj(_regionCharacters, out objectSelected, i);
                                HOperatorSet.ConcatObj(newRegion, objectSelected, out newRegion);
                            }
                            break;
                        }
                    }
                    if (flg)
                    {
                        break;
                    }
                }
                if (!flg)
                {
                    // 結合なし
                    HOperatorSet.ConcatObj(newRegion, oldRegion, out newRegion);
                }
                if (!flg && i == number)
                {
                    HOperatorSet.ConcatObj(newRegion, objectSelected, out newRegion);
                }
                oldColumns = columns;
                HOperatorSet.CopyObj(objectSelected, out oldRegion, 1, 1);
                oldIndex = i;

                // for debug
                //HOperatorSet.GenImageConst(out HObject image, "byte", 1024, 512);
                //HOperatorSet.OverpaintRegion(image, objectSelected, 255, "fill");
                //HOperatorSet.WriteImage(image, "tiff", 0, "aaa.tif");
            }

            // for debug
            //HOperatorSet.CountObj(newRegion, out HTuple nubm);

            HOperatorSet.SortRegion(newRegion, out HObject sortedRegions, "character", "true", "row");

            // 表示
            hSmartWindowControl1.HalconWindow.DispObj(_image);
            hSmartWindowControl1.HalconWindow.DispObj(sortedRegions);
        }
    }
}
