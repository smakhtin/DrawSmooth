using System;
using VVVV.Utils.VMath;
using SlimDX;
using VVVV.Lib;

namespace VVVV.Nodes
{
	public class Triangle
	{
		public Microsoft.DirectX.Direct3D.CustomVertex.PositionColoredTextured[] Vertices = new Microsoft.DirectX.Direct3D.CustomVertex.PositionColoredTextured[3];

		public Vertex[] MeshVertices = new Vertex[3];

		public Vector2 BasePoint;

		public Triangle(float x1, float y1, float z1,
			float x2, float y2, float z2,
			float x3, float y3, float z3, int color)
		{
			Vertices[0].Position = new Microsoft.DirectX.Vector3(
				Convert.ToSingle(VMath.Map(x1, 0, DrawSmoother2D.Width, -1, 1, TMapMode.Float)),
				Convert.ToSingle(VMath.Map(DrawSmoother2D.Height - y1, 0, DrawSmoother2D.Height, -1, 1, TMapMode.Float)), z1);

			Vertices[0].Color = color;
			Vertices[0].Tu = Convert.ToSingle(VMath.Map(x1, 0, DrawSmoother2D.Width, 0, 1, TMapMode.Float)); // 0f;
			Vertices[0].Tv = Convert.ToSingle(VMath.Map(y1, 0, DrawSmoother2D.Height, 0, 1, TMapMode.Float)); // 0.5f;

			Vertices[1].Position = new Microsoft.DirectX.Vector3(
				Convert.ToSingle(VMath.Map(x2, 0, DrawSmoother2D.Width, -1, 1, TMapMode.Float)),
				Convert.ToSingle(VMath.Map(DrawSmoother2D.Height - y2, 0, DrawSmoother2D.Height, -1, 1, TMapMode.Float)), z2);

			Vertices[1].Color = color;
			Vertices[1].Tu = Convert.ToSingle(VMath.Map(x2, 0, DrawSmoother2D.Width, 0, 1, TMapMode.Float)); // 1.0f;
			Vertices[1].Tv = Convert.ToSingle(VMath.Map(y2, 0, DrawSmoother2D.Height, 0, 1, TMapMode.Float)); // 0.0f;

			Vertices[2].Position = new Microsoft.DirectX.Vector3(
				Convert.ToSingle(VMath.Map(x3, 0, DrawSmoother2D.Width, -1, 1, TMapMode.Float)),
				Convert.ToSingle(VMath.Map(DrawSmoother2D.Height - y3, 0, DrawSmoother2D.Height, -1, 1, TMapMode.Float)), z3);

			Vertices[2].Color = color;
			Vertices[2].Tu = Convert.ToSingle(VMath.Map(x3, 0, DrawSmoother2D.Width, 0, 1, TMapMode.Float)); // 1.0f;
			Vertices[2].Tv = Convert.ToSingle(VMath.Map(y3, 0, DrawSmoother2D.Height, 0, 1, TMapMode.Float)); // 1.0f;
		}

		public bool ContainsPoint(float x, float y)
		{
			return x == BasePoint.X && y == BasePoint.Y;
		}
	}

}
