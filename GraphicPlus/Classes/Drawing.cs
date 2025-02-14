﻿using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicPlus
{
    public class Drawing
    {

        #region members


        protected List<Shape> shapes = new List<Shape>();

        protected double width = 0;
        protected double height = 0;
        protected int dpi = 96;

        protected Rectangle3d boundary = Rectangle3d.Unset;

        public Color Background = Color.Transparent;

        public int digits = 4;

        #endregion

        #region constructors

        public Drawing()
        {

        }

        public Drawing(Shape shape)
        {
            BoundingBox box = shape.GetBoundingBox();
            Rectangle3d rect = box.ToRectangle();
            this.boundary = rect;
            this.width = rect.Width;
            this.height = rect.Height;
            this.shapes.Add(shape);
        }

        public Drawing(Drawing drawing)
        {
            this.boundary = drawing.boundary;
            this.width = drawing.width;
            this.height = drawing.height;
            this.Background = drawing.Background;
            this.shapes = drawing.shapes;
            this.dpi = drawing.dpi;
        }

        public Drawing(List<Shape> shapes, Rectangle3d boundary, double width, double height)
        {
            foreach (Shape shape in shapes)
            {
                this.shapes.Add(new Shape(shape));
            }

            this.boundary = boundary;
            this.width = width;
            this.height = height;
        }

        public Drawing(List<Shape> shapes)
        {
            BoundingBox box = BoundingBox.Unset;
            foreach (Shape shape in shapes)
            {
                this.shapes.Add(new Shape(shape));
                box.Union(shape.GetBoundingBox());
            }

            this.boundary = box.ToRectangle();
            this.width = this.boundary.Width;
            this.height = this.boundary.Height;
        }

        #endregion

        #region properties

        public virtual int Dpi
        {
            get { return dpi; }
            set { dpi = Math.Max(value, 72); }
            }

        public virtual List<Shape> Shapes
        {
            get {
                List<Shape> output = new List<Shape>();
                foreach(Shape shape in shapes)
                {
                    output.Add(new Shape(shape));
                }
                return output;
            }
        }

        public virtual Rectangle3d Boundary
        {
            get { return new Rectangle3d(boundary.Plane,boundary.Corner(0),boundary.Corner(2)); }
        }

        public virtual double Scale
        {
            get
            {
                return Math.Min(width / boundary.Width, height / boundary.Height);
            }
        }

        public virtual double TranslateX
        {
            get
            {
                double t = 0;
                if (Scale != 1)
                {
                    if (Math.Abs(width / boundary.Width) > Math.Abs(height / boundary.Height))
                    {
                        t = Math.Round((width - boundary.Width * Scale) / -2.0, digits);
                    }
                }
                return t;
            }
        }

        public virtual double TranslateY
        {
            get
            {
                double t = 0;
                if (Scale != 1)
                {
                    if (Math.Abs(height / boundary.Height) > Math.Abs(width / boundary.Width))
                    {
                        t = Math.Round((height - boundary.Height * Scale) / -2.0, digits);
                    }
                }
                return t;
            }
        }

        public virtual double Width
        {
            get { return width; }
        }

        public virtual double Height
        {
            get { return height; }
        }

        #endregion

        #region methods

        public void MergeDrawing(List<Shape> shapes)
        {
            MergeDrawing(new Drawing(shapes));
        }

        public void MergeDrawing(Shape shape)
        {
            MergeDrawing(new Drawing(shape));
        }

            public void MergeDrawing(Drawing drawing)
        {
            foreach (Shape shape in drawing.shapes)
            {
                this.shapes.Add(new Shape(shape));
            }

            BoundingBox box = drawing.boundary.BoundingBox;
            box.Union(this.boundary.BoundingBox);

            if (drawing.Background != Color.Transparent) this.Background = drawing.Background;
            this.boundary = box.ToRectangle();
            this.width = Math.Max(drawing.width, this.width);
            this.height = Math.Max(drawing.height, this.height);
        }

        public string ToScript()
        {
            StringBuilder output = new StringBuilder();

            double W = Math.Round(width, digits);
            double H = Math.Round(height, digits);

            double S = Math.Round(Scale, digits);
            double X = Math.Round(TranslateX,digits);
            double Y = Math.Round(TranslateY, digits);

            //Setup Canvas
            output.Append(Properties.Resources.svg_header);
            output.Append("width=\"" + W + "\" height=\"" + H + "\" ");
            output.Append("viewBox = \" " + X + " " + Y + " " + W + " " + H + "\" ");
            output.Append("preserveAspectRatio=\"xMinYMin meet\"");
            output.AppendLine(">");

            string color = ColorTranslator.ToHtml(Background);
            if (Background.A == 0) color = "none";
            output.AppendLine("<rect id=\"background\" x=\""+X+"\" y=\""+Y+"\" width=\"" + W + "\" height=\"" + H + "\" style=\"fill:"+color+"; stroke:none;\" />");

            StringBuilder defs = new StringBuilder();
            defs.AppendLine("<defs>");
            int i = 0;
            List<int> graphicIds = new List<int>();
            //Draw Geometry
            foreach(Shape shape in shapes)
            {
                output.AppendLine(shape.ToScript(boundary, S));
                if (!graphicIds.Contains(shape.Graphics.GetHashCode()))
                {
                    switch (shape.PathType)
                    {
                        case Shape.PathTypes.Text:
                            graphicIds.Add(shape.Graphics.GetHashCode());
                            defs.AppendLine(shape.Graphics.ToScript(6,true));
                            if (shape.Graphics.PostEffect.EffectType != Effect.EffectTypes.None) defs.AppendLine(shape.Graphics.ToSVGEffect());
                            break;
                        default:
                            graphicIds.Add(shape.Graphics.GetHashCode());
                            defs.AppendLine(shape.Graphics.ToScript());
                            if (shape.Graphics.FillType == Graphic.FillTypes.LinearGradient) defs.AppendLine(shape.Graphics.GetLinearGradientScript());
                            if (shape.Graphics.FillType == Graphic.FillTypes.RadialGradient) defs.AppendLine(shape.Graphics.GetRadialGradientScript());
                            if (shape.Graphics.PostEffect.EffectType != Effect.EffectTypes.None) defs.AppendLine(shape.Graphics.ToSVGEffect());
                            break;
                    }
                }
                i++;
            }
            defs.AppendLine("</defs>");


            output.Append(defs.ToString());
            output.Append("</svg>");

            return output.ToString();
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return "Drawing: (w:"+Math.Round(Width,2)+" h:"+Math.Round(Height,2)+" s:"+Shapes.Count+")";
        }

        #endregion

    }
}
