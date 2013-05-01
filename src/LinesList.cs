using System.Collections.Generic;
using VVVV.Utils.VMath;
using VVVV.Utils.VColor;

namespace VVVV.Nodes
{
	public class LinesList
	{
		public List<SmoothLine> ActualSmoothLines = new List<SmoothLine>();
		
		public List<SmoothLine> BreakedSmoothLines = new List<SmoothLine>();
		
		private List<int> FFRameCounts = new List<int>();
		
		private List<bool> FAllowWrite = new List<bool>();
		
		private List<int> FSmoothPointsCount = new List<int>();
		
		private List<bool> FAllowBreak = new List<bool>();
		
		private List<RGBAColor> FColors = new List<RGBAColor>();
		
		private List<int> FLineWidth = new List<int>();

		private List<int> FPointsRange = new List<int>();

		public void NewColors(List<RGBAColor> lcol)
		{
			FColors = lcol;
		}

		public void NewBreakPoints(List<bool> allowBreak)
		{
			FAllowBreak = allowBreak;
		}

		public void NewPoints(List<Vector2D> points)
		{
			if (FFRameCounts.Count == 0) return;
			if (FAllowWrite.Count == 0) return;
			if (FSmoothPointsCount.Count == 0) return;
			if (FAllowBreak.Count == 0) return;
			if (FColors.Count == 0) return;

			if (points.Count == 0) return;

			if (ActualSmoothLines.Count < points.Count)
			{
				for (int i = ActualSmoothLines.Count; i < points.Count; i++)
					ActualSmoothLines.Add(new SmoothLine(FFRameCounts[i % FFRameCounts.Count],
											   FSmoothPointsCount[i % FSmoothPointsCount.Count],
											   FColors[i % FColors.Count],
											   FLineWidth[i % FLineWidth.Count],
											   FPointsRange[i % FPointsRange.Count]));
			}
			
			//Aligning points
			for (int i = 0; i < points.Count; i++)
			{
				if (FAllowWrite[i % FAllowWrite.Count] && FAllowBreak[i % FAllowBreak.Count] == false)
					ActualSmoothLines[i].AddNewPoint(points[i]);
			}
		}

		//Creating points list
		public void SetFrameCounts(List<int> frameCounts)
		{
			if (frameCounts == null) return;
			FFRameCounts = frameCounts;

			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].SetFrameCount(FFRameCounts[i % FFRameCounts.Count]);
		}

		public void SetWrite(List<bool> write)
		{
			if (write == null) return;
			FAllowWrite = write;
		}

		public void SetSmoothPointsCount(List<int> smoothPointsCounts)
		{
			if (smoothPointsCounts == null) return;
			FSmoothPointsCount = smoothPointsCounts;

			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].SetSmoother(FSmoothPointsCount[i % FSmoothPointsCount.Count]);
		}

		public void SetBreakPoints(List<bool> bp)
		{
			if (bp == null) return;
			
			FAllowBreak = bp;
			
			for (int i = 0; i < ActualSmoothLines.Count; i++)
			{
				if (FAllowBreak[i % FAllowBreak.Count])
				{
					if (ActualSmoothLines[i].Flagnew == false)
					{
						AddBreakLine(ActualSmoothLines[i]);
						ActualSmoothLines[i] = new SmoothLine(FFRameCounts[i % FFRameCounts.Count],
												FSmoothPointsCount[i % FSmoothPointsCount.Count],
												FColors[i % FColors.Count],
												FLineWidth[i % FLineWidth.Count],
												FPointsRange[i % FPointsRange.Count]);
					}
				}
				ActualSmoothLines[i].SetColor(FColors[i % FColors.Count]);
			}
		}

		public void SetColors(List<RGBAColor> c)
		{
			if (c == null) return;
			FColors = c;
	
			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].SetColor(FColors[i % FColors.Count]);
		}

		public void SetLinesWidth(List<int> linesWidth)
		{
			if (linesWidth == null) return;
			FLineWidth = linesWidth;
			
			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].LineWidth = FLineWidth[i % FLineWidth.Count];
		}

		public void SetPointsRange(List<int> pointsRange)
		{
			if (pointsRange == null) return;
			FPointsRange = pointsRange;

			for (int i = 0; i < ActualSmoothLines.Count; i++)
				ActualSmoothLines[i].PointsRange = FPointsRange[i % FPointsRange.Count];
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
			int slice = 0;
			
			foreach (SmoothLine t in BreakedSmoothLines)
			{
				for (int j = 0; j < t.SmoothLineParticles.Count; j += t.PointsRange)
				{
					slice++;
				}
			}

			foreach (SmoothLine t in ActualSmoothLines)
			{
				for (int j = 0; j < t.SmoothLineParticles.Count; j += t.PointsRange)
				{
					slice++;
				}
			}

			return slice;
		}

		public void Reset()
		{
			foreach (SmoothLine t in ActualSmoothLines) t.Reset();
			ActualSmoothLines.Clear();
			
			foreach (SmoothLine t in BreakedSmoothLines) t.Reset();
			BreakedSmoothLines.Clear();
		}
	}
}
