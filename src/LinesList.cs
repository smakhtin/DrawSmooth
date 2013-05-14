using System.Collections.Generic;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Utils.VColor;

namespace VVVV.Nodes
{
	public class LinesList
	{
		public List<SmoothLine> ActualSmoothLines = new List<SmoothLine>();
		
		public List<SmoothLine> BreakedSmoothLines = new List<SmoothLine>();
		
		public ISpread<int> FrameCounts { get; set; }

		public ISpread<bool> AllowWrite { get; set; }
		
		public ISpread<int> SmoothPointsCount { get; set; }
		
		public ISpread<bool> AllowBreak { get; set; }
		
		public ISpread<RGBAColor> Colors { get; set; }
		
		public ISpread<int> LineWidth { get; set; }

		public ISpread<int> PointsRange { get; set; }

		public void NewPoints(ISpread<Vector2D> points)
		{
			if (ActualSmoothLines.Count < points.SliceCount)
			{
				for (var i = ActualSmoothLines.Count; i < points.SliceCount; i++)
				{
					ActualSmoothLines.Add(new SmoothLine(FrameCounts[i % FrameCounts.SliceCount], SmoothPointsCount[i % SmoothPointsCount.SliceCount], Colors[i % Colors.SliceCount], LineWidth[i % LineWidth.SliceCount], PointsRange[i % PointsRange.SliceCount]));
				}
					
			}
			
			for (var i = 0; i < points.SliceCount; i++)
			{
				if (AllowWrite[i % AllowWrite.SliceCount] && AllowBreak[i % AllowBreak.SliceCount] == false) ActualSmoothLines[i].AddNewPoint(points[i]);
			}
		}

		//Creating points list
		public void SetFrameCounts(ISpread<int> frameCounts)
		{
			if (frameCounts == null) return;
			FrameCounts = frameCounts;

			for (var i = 0; i < ActualSmoothLines.Count; i++) ActualSmoothLines[i].SetFrameCount(FrameCounts[i % FrameCounts.SliceCount]);
		}

		public void SetWrite(ISpread<bool> write)
		{
			if (write == null) return;
			
			AllowWrite = write;
		}

		public void SetSmoothPointsCount(ISpread<int> smoothPointsCounts)
		{
			if (smoothPointsCounts == null) return;
			SmoothPointsCount = smoothPointsCounts;

			for (var i = 0; i < ActualSmoothLines.Count; i++)
			{
				ActualSmoothLines[i].SetSmoother(SmoothPointsCount[i % SmoothPointsCount.SliceCount]);
			}
				
		}

		public void SetBreakPoints(ISpread<bool> breakPoints)
		{
			if (breakPoints == null) return;
			
			AllowBreak = breakPoints;
			
			for (var i = 0; i < ActualSmoothLines.Count; i++)
			{
				if (AllowBreak[i % AllowBreak.SliceCount])
				{
					if (ActualSmoothLines[i].Flagnew == false)
					{
						AddBreakLine(ActualSmoothLines[i]);
						ActualSmoothLines[i] = new SmoothLine(FrameCounts[i % FrameCounts.SliceCount],
												SmoothPointsCount[i % SmoothPointsCount.SliceCount],
												Colors[i % Colors.SliceCount],
												LineWidth[i % LineWidth.SliceCount],
												PointsRange[i % PointsRange.SliceCount]);
					}
				}
				ActualSmoothLines[i].SetColor(Colors[i % Colors.SliceCount]);
			}
		}

		public void SetColors(ISpread<RGBAColor> c)
		{
			if (c == null) return;
			Colors = c;
	
			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].SetColor(Colors[i % Colors.SliceCount]);
		}

		public void SetLinesWidth(ISpread<int> linesWidth)
		{
			if (linesWidth == null) return;
			LineWidth = linesWidth;
			
			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].LineWidth = LineWidth[i % LineWidth.SliceCount];
		}

		public void SetPointsRange(ISpread<int> pointsRange)
		{
			if (pointsRange == null) return;
			PointsRange = pointsRange;

			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].PointsRange = PointsRange[i % PointsRange.SliceCount];
		}

		public void AddBreakLine(SmoothLine line)
		{
			line.InputLine.Clear();
			line.TriangleList.Clear();
			BreakedSmoothLines.Add(line);
		}

		public int GetAllSlicesTransform()
		{
			int slice = 0;
			for (int i = 0; i < ActualSmoothLines.Count; i++)
				slice += ActualSmoothLines[i].GetAllParticles();
			for (int i = 0; i < BreakedSmoothLines.Count; i++)
				slice += BreakedSmoothLines[i].GetAllParticles();
			return slice;
		}

		public int GetAllPoints()
		{
			var slice = 0;
			
			foreach (var t in BreakedSmoothLines)
			{
				for (var j = 0; j < t.SmoothLineParticles.Count; j += t.PointsRange)
				{
					slice++;
				}
			}

			foreach (var t in ActualSmoothLines)
			{
				for (var j = 0; j < t.SmoothLineParticles.Count; j += t.PointsRange)
				{
					slice++;
				}
			}

			return slice;
		}

		public void Reset()
		{
			foreach (var t in ActualSmoothLines) t.Reset();
			ActualSmoothLines.Clear();
			
			foreach (var t in BreakedSmoothLines) t.Reset();
			BreakedSmoothLines.Clear();
		}
	}
}
