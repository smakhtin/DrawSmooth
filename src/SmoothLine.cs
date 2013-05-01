using System;
using System.Collections.Generic;
using DrawSmoother;
using SlimDX;
using VVVV.Lib;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

namespace VVVV.Nodes
{
	//One line
	public class SmoothLine
	{
		//Source Line
		public List<Vector2D> InputLine = new List<Vector2D>();

		//Smooth Line
		public List<Particle> SmoothLineParticles = new List<Particle>();

		//Triangles for rendering
		public List<Triangle> TriangleList = new List<Triangle>();

		//Vertexes for Mesh
		public List<Vertex> Vertices = new List<Vertex>();

		//Indexes for Mesh
		public List<short> Indexes = new List<short>();

		//Latest added point
		private Vector2D FLastPoint;

		//Points limit
		private int FFRameCount;

		//Number of smooth points
		private int FSmoother = 50;

		public int PointsRange = 20;

		public RGBAColor ColorOut;

		public float LineWidth = 1f;

		//Flag of the new line
		public bool Flagnew = true;

		private const float DeltaZ = 0.0000001f;
		private float FCurrentZ;

		public float CurrentZ
		{
			get
			{
				FCurrentZ += DeltaZ;
				return FCurrentZ;
			}
		}

		public SmoothLine(int framecount, int smoother, RGBAColor col, int lineWidth, int pointsrange)
		{
			FFRameCount = framecount;
			FSmoother = smoother;
			ColorOut = col;
			LineWidth = lineWidth;
			PointsRange = pointsrange;
		}

		public void AddNewPoint(Vector2D point)
		{
			try
			{
				//Checking point for it's actuality
				if (FLastPoint.x == point.x && FLastPoint.y == point.y) return;

				double length = Math.Sqrt((FLastPoint.x - point.x) * (FLastPoint.x - point.x) +
										  (FLastPoint.y - point.y) * (FLastPoint.y - point.y));
				//Checking delta
				if (length < 6) return;

				FLastPoint = point;

				InputLine.Add(point);

				//Calculating spline points
				if (InputLine.Count >= 4)
				{
					//Points order (x_1;y_1)  (x0;y0)   (x1;y1)  (x2;y2) 
					int cnt = InputLine.Count - 1;
					double x_1 = InputLine[cnt - 3].x;
					double x0 = InputLine[cnt - 2].x;
					double x1 = InputLine[cnt - 1].x;
					double x2 = InputLine[cnt - 0].x;
					double y_1 = InputLine[cnt - 3].y;
					double y0 = InputLine[cnt - 2].y;
					double y1 = InputLine[cnt - 1].y;
					double y2 = InputLine[cnt - 0].y;

					//Calculating coefficients
					double a3 = (-x_1 + 3 * x0 - 3 * x1 + x2) / 6d;
					double a2 = (x_1 - 2 * x0 + x1) / 2d;
					double a1 = (-x_1 + x1) / 2d;
					double a0 = (x_1 + 4 * x0 + x1) / 6d;
					double b3 = (-y_1 + 3 * y0 - 3 * y1 + y2) / 6d;
					double b2 = (y_1 - 2 * y0 + y1) / 2d;
					double b1 = (-y_1 + y1) / 2d;
					double b0 = (y_1 + 4 * y0 + y1) / 6d;

					//Filling interval
					double delta = 1d / (FSmoother + 1);
					for (double step = delta; step < 1; step += delta)
					{
						double x = ((a3 * step + a2) * step + a1) * step + a0;
						double y = ((b3 * step + b2) * step + b1) * step + b0;
						if (SmoothLineParticles.Count == 0)
						{
							//Adding point
							SmoothLineParticles.Add(new Particle(Convert.ToSingle(x), Convert.ToSingle(y)));
						}
						else
						{
							length = Math.Sqrt((SmoothLineParticles[SmoothLineParticles.Count - 1].x - x) * (SmoothLineParticles[SmoothLineParticles.Count - 1].x - x) +
										  (SmoothLineParticles[SmoothLineParticles.Count - 1].y - y) * (SmoothLineParticles[SmoothLineParticles.Count - 1].y - y));

							//Checking delta
							if (2 > length) continue;

							//Adding new point
							SmoothLineParticles.Add(new Particle(Convert.ToSingle(x), Convert.ToSingle(y)));

							GetLastTwoPoint();
							int index = SmoothLineParticles.Count - 2;
							Vector2 a = new Vector2(SmoothLineParticles[index].x1, SmoothLineParticles[index].y1);
							Vector2 b = new Vector2(SmoothLineParticles[index].x2, SmoothLineParticles[index].y2);
							Vector2 c = new Vector2(SmoothLineParticles[index + 1].x1, SmoothLineParticles[index + 1].y1);
							Vector2 d = new Vector2(SmoothLineParticles[index + 1].x2, SmoothLineParticles[index + 1].y2);
							if (Intersect(a, b, c, d))
							{
								float tmpx = SmoothLineParticles[index + 1].x1;
								float tmpy = SmoothLineParticles[index + 1].y1;
								SmoothLineParticles[index + 1].x1 = SmoothLineParticles[index + 1].x2;
								SmoothLineParticles[index + 1].y1 = SmoothLineParticles[index + 1].y2;
								SmoothLineParticles[index + 1].x2 = tmpx;
								SmoothLineParticles[index + 1].y2 = tmpy;
							}
							AddNewTriangle();
						}
					}
				}
				//Clearing extra points
				if (FFRameCount > 0)
				{
					if (InputLine.Count > FFRameCount)
					{
						//Yes, we have extra points
						if (InputLine.Count != 0)
							InputLine.RemoveAt(0);
						if (SmoothLineParticles.Count > FSmoother)
						{
							for (int i = 0; i <= FSmoother; i++)
								DeleteTriangle(SmoothLineParticles[i].x, SmoothLineParticles[i].y);
							SmoothLineParticles.RemoveRange(0, FSmoother);
						}
					}
				}
				Flagnew = false;
			}
			catch (Exception ex)
			{
				//DrawSmoother2D.instance_.FLogger.Log(LogType.Message,  "AddNewPoint: " + ex.Message);
			}
		}

		//Calculating triangles
		public void GetLastTwoPoint()
		{
			if (SmoothLineParticles.Count < 2) return;
			int index = SmoothLineParticles.Count - 2;
			float x1 = SmoothLineParticles[index].x;
			float y1 = SmoothLineParticles[index].y;
			float x2 = SmoothLineParticles[index + 1].x;
			float y2 = SmoothLineParticles[index + 1].y;

			double len = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
			double alfa;

			if (len != 0) alfa = Math.Acos((x2 - x1) / len);
			else alfa = Math.PI / 2;

			if (y2 - y1 < 0) alfa = Math.PI - alfa;

			alfa += Math.PI / 2;

			float deltax = Convert.ToSingle(LineWidth / 2d * Math.Cos(alfa));
			float deltay = Convert.ToSingle(LineWidth / 2d * Math.Sin(alfa));
			SmoothLineParticles[index + 1].x1 = x2 + deltax;
			SmoothLineParticles[index + 1].y1 = y2 + deltay;
			SmoothLineParticles[index + 1].x2 = x2 - deltax;
			SmoothLineParticles[index + 1].y2 = y2 - deltay;
		}

		//Adding triangle to mesh
		private void AddMesh(Triangle triangle)
		{
			for (int i = 0; i < 3; i++)
			{
				Vertex vertex = new Vertex
				                	{
				                		pv = new Vector3(triangle.Vertices[i].X, triangle.Vertices[i].Y, triangle.Vertices[i].Z),
				                		nv = new Vector3(0.0f, 0.0f, 1.0f),
				                		tu1 = 0.5f - triangle.Vertices[i].X,
				                		tv1 = 0.5f - triangle.Vertices[i].Y
				                	};

				Vertices.Add(vertex);
			}

			Indexes.Add(Convert.ToInt16((Vertices.Count - 3)));
			Indexes.Add(Convert.ToInt16((Vertices.Count - 2)));
			Indexes.Add(Convert.ToInt16((Vertices.Count - 1)));
		}

		#region Checking for intersection
		private float square(Vector2 a, Vector2 b, Vector2 c)
		{
			return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);
		}

		private bool intersect_1(float a, float b, float c, float d)
		{
			return Math.Max(a, b) >= Math.Min(c, d) && Math.Max(c, d) >= Math.Min(a, b);
		}

		private bool Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		{
			float s11 = square(a, b, c);
			float s12 = square(a, b, d);
			float s21 = square(c, d, a);
			float s22 = square(c, d, b);
			if (s11 == 0 && s12 == 0 && s21 == 0 && s22 == 0)
				return intersect_1(a.X, b.X, c.X, d.X) && intersect_1(a.Y, b.Y, c.Y, d.Y);
			
			return (s11 * s12 <= 0) && (s21 * s22 <= 0);
		}
		#endregion

		private void AddTriangle(Vector2 a, Vector2 b, Vector2 c)
		{
			Triangle triangle = new Triangle(a.X, a.Y, CurrentZ, b.X, b.Y, CurrentZ, c.X, c.Y, CurrentZ, ColorOut.Color.ToArgb())
			                    	{
			                    		BasePoint =
			                    			new Vector2(SmoothLineParticles[SmoothLineParticles.Count - 1].x,
			                    			            SmoothLineParticles[SmoothLineParticles.Count - 1].y)
			                    	};
			AddMesh(triangle);
			TriangleList.Add(triangle);
		}

		private void AddNewTriangle()
		{
			if (SmoothLineParticles.Count < 3) return;
			int index = SmoothLineParticles.Count - 2;

			Vector2 a = new Vector2(SmoothLineParticles[index].x1, DrawSmoother2D.Height - SmoothLineParticles[index].y1);
			Vector2 b = new Vector2(SmoothLineParticles[index].x2, DrawSmoother2D.Height - SmoothLineParticles[index].y2);
			Vector2 c = new Vector2(SmoothLineParticles[index + 1].x1, DrawSmoother2D.Height - SmoothLineParticles[index + 1].y1);
			Vector2 d = new Vector2(SmoothLineParticles[index + 1].x2, DrawSmoother2D.Height - SmoothLineParticles[index + 1].y2);

			if (!Intersect(a, b, c, d))
			{
				if (!Intersect(a, c, b, d))
				{
					AddTriangle(a, d, b);
					AddTriangle(a, d, c);
				}
				if (!Intersect(a, d, b, c))
				{
					AddTriangle(a, c, b);
					AddTriangle(a, c, d);
				}
			}
			else
			{
				if (!Intersect(a, c, b, d))
				{
					AddTriangle(c, d, a);
					AddTriangle(c, d, b);
				}
				if (!Intersect(a, d, b, c))
				{
					AddTriangle(d, c, a);
					AddTriangle(d, c, b);
				}
			}
		}

		public void SetFrameCount(int framecount)
		{
			if (framecount >= 0)
				FFRameCount = framecount;
		}

		//Set the number of smooth points
		public void SetSmoother(int smoother)
		{
			if (smoother > 0)
				FSmoother = smoother;
		}

		//Output color
		public void SetColor(RGBAColor col)
		{
			ColorOut = col;
		}

		//Particles count
		public int GetAllParticles()
		{
			return SmoothLineParticles.Count;
		}

		//Delet triangle, that contains point
		public void DeleteTriangle(float x, float y)
		{
			for (int i = 0; i < TriangleList.Count; i++)
			{
				if (!TriangleList[i].ContainsPoint(x, y)) continue;
				
				TriangleList.RemoveAt(i);
				i--;
			}
		}

		public void Reset()
		{
			InputLine.Clear();
			SmoothLineParticles.Clear();
			TriangleList.Clear();
			Vertices.Clear();
			Indexes.Clear();
			FLastPoint = new Vector2D();
		}
	}
}

