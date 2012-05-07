using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.PluginInterfaces.V1;
using VVVV.Lib;
using SlimDX;
using SlimDX.Direct3D9;

namespace VVVV.Nodes
{
	//custom data per graphics device
	public class CustomDeviceData : DeviceData
	{
		//texture for this device
		//public Texture Tex { get; set; }

		//vertex buffer for this device
		//public VertexBuffer Vb { get; set; }

		public Device Device { get; private set; }

		public CustomDeviceData(Device device)
		{
			Device = device;
		}
	}

	[PluginInfo(Name = "DrawSmooth", Author = "bo27", Credits = "alg", Category = "DX9", Help = "Draws smooth lines")]
	public class DrawSmoother2D : DXLayerOutPluginBase<CustomDeviceData>, IPluginEvaluate
	{
		[Input("FrameCount", DefaultValue = 20, Visibility = PinVisibility.Hidden)] private IDiffSpread<int> FFrameCount;
		[Input("Transform")] private ISpread<Matrix> FWorldTransform;
		[Input("")] private IDiffSpread<Vector2D> FXY;
		[Input("Insert")] private IDiffSpread<bool> FWrite;
		[Input("Width")] private IDiffSpread<int> FWidthLine;
		[Input("Smoother")] private IDiffSpread<int> FSmoother;
		[Input("Color")] private IDiffSpread<RGBAColor> FColors;

		public static DrawSmoother2D Instance;

		public static int Width = 200;
		public static int Height = 200;

		private readonly LinesList FAllLines = new LinesList();

		readonly List<int> FFc = new List<int>();
		readonly List<bool> FW = new List<bool>();
		readonly List<int> FS = new List<int>();
		readonly List<bool> FBp = new List<bool>();

		readonly List<RGBAColor> FColor = new List<RGBAColor>();

		readonly List<int> FWl = new List<int>();
		readonly List<int> FPr = new List<int>();

		private readonly Dictionary<Device, Mesh> FMeshes = new Dictionary<Device, Mesh>();

		#region field declaration
		public IPluginHost Host;

		//input pin declaration
		private IValueIn FBreakPoint;
		private IValueIn FPointsRange;
		private IValueIn FReset;

		//output pin declaration
		private IDXLayerIO FLayerOutput;
		#endregion field declaration

		[ImportingConstructor]
		public DrawSmoother2D(IPluginHost host)
			: base(host, true, true)
		{
			SetPluginHost(host);
		}

		#region pin creation
		//this method is called by vvvv when the node is created
		public void SetPluginHost(IPluginHost host)
		{
			//assign host
			Host = host;
			Instance = this;
			
			//create inputs
			

			Host.CreateValueInput("SamplingPoints", 1, null, TSliceMode.Dynamic, TPinVisibility.True, out FPointsRange);
			FPointsRange.SetSubType(0, 1000000, 1, 20, false, false, true);

			Host.CreateValueInput("Break", 1, null, TSliceMode.Dynamic, TPinVisibility.Hidden, out FBreakPoint);
			FBreakPoint.SetSubType(0, 1, 1, 0, true, true, true);

			Host.CreateValueInput("Reset", 1, null, TSliceMode.Single, TPinVisibility.True, out FReset);
			FReset.SetSubType(0, 1, 1, 0, true, false, true);

			//create outputs	    
			Host.CreateLayerOutput("Layer", TPinVisibility.True, out FLayerOutput);
		}
		#endregion pin creation

		#region DXLayer
		protected override void Render(Device device, CustomDeviceData deviceData)
		{
			FRenderStatePin.SetSliceStates(0);
			device.SetTransform(TransformState.World, FWorldTransform[0]);

			Width = device.Viewport.Width;
			Height = device.Viewport.Height;

			device.VertexFormat = (VertexFormat)Microsoft.DirectX.Direct3D.CustomVertex.PositionColoredTextured.Format;

			foreach (SmoothLine line in FAllLines.ActualSmoothLines)
			{
				lock (line.TriangleList)
				{
					foreach (Triangle triangle in line.TriangleList)
					{
						device.DrawUserPrimitives(PrimitiveType.TriangleList, 1, triangle.Vertices);
					}
				}
			}
		}

		public override void SetStates()
		{
			FRenderStatePin.SetRenderState(RenderState.AlphaTestEnable, 1);
			FRenderStatePin.SetRenderState(RenderState.SourceBlend, 1);
			FRenderStatePin.SetRenderState(RenderState.DestinationBlend, 1);
		}
		#endregion

		#region mainloop

		public void Configurate(IPluginConfig input)
		{
			//nothing to configure in this plugin
			//only used in conjunction with inputs of type cmpdConfigurate
		}

		//here we go, thats the method called by vvvv each frame
		//all data handling should be in here
		public void Evaluate(int spreadMax)
		{
			#region количество разрешенных точек линий
			if (FFrameCount.IsChanged)
			{
				FFc.Clear();
				for (int i = 0; i < FFrameCount.SliceCount; i++)
				{
					double a = FFrameCount[i];
					if (a < 0) a = 0;
					FFc.Add(Convert.ToInt32(a));
				}
				if (FFc.Count != 0)
					FAllLines.SetFrameCounts(FFc);
			}
			#endregion

			#region список разрешений работы
			if (FWrite.IsChanged)
			{
				FW.Clear();
				for (int i = 0; i < FWrite.SliceCount; i++)
				{
					FW.Add(FWrite[i]);
				}
				if (FW.Count != 0)
					FAllLines.SetWrite(FW);
			}
			#endregion

			#region список количества точек сглаживания
			if (FSmoother.IsChanged)
			{
				FS.Clear();
				for (int i = 0; i < FSmoother.SliceCount; i++)
				{
					int a = FSmoother[i];
					a += 1;
					if (a < 0) a = 1;
					FS.Add(a);
				}
				if (FS.Count != 0)
					FAllLines.SetSmoothPointsCount(FS);
			}
			#endregion

			#region список разрешений на прерывание линии
			if (FBreakPoint.PinIsChanged)
			{
				FBp.Clear();
				for (int i = 0; i < FBreakPoint.SliceCount; i++)
				{
					double a;
					FBreakPoint.GetValue(i, out a);
					FBp.Add(Convert.ToBoolean(a));
				}
				if (FBp.Count != 0)
					FAllLines.SetBreakPoints(FBp);
			}
			#endregion

			#region список цветов
			if (FColors.IsChanged)
			{
				FColor.Clear();
				for (int i = 0; i < FColors.SliceCount; i++)
				{
					FColor.Add(FColors[i]);
				}
				if (FColor.Count != 0)
					FAllLines.SetColors(FColor);
			}
			#endregion

			#region список толщины линий
			if (FWidthLine.IsChanged)
			{
				FWl.Clear();
				for (int i = 0; i < FWidthLine.SliceCount; i++)
				{
					FWl.Add(FWidthLine[i]);
				}
				if (FWl.Count != 0)
					FAllLines.SetLinesWidth(FWl);
			}
			#endregion

			#region количество пропускаемых точек для вывода
			if (FPointsRange.PinIsChanged)
			{
				FPr.Clear();
				for (int i = 0; i < FPointsRange.SliceCount; i++)
				{
					double a;
					FPointsRange.GetValue(i, out a);
					FPr.Add(Convert.ToInt32(a));
				}
				if (FPr.Count != 0)
					FAllLines.SetPointsRange(FPr);
			}
			#endregion

			#region Reset
			if (FReset.PinIsChanged)
			{
				double a;
				FReset.GetValue(0, out a);
				if (a == 1)
					FAllLines.Reset();
				FMeshes.Clear();
			}
			#endregion

			#region новые координаты
			if (FXY.IsChanged)
			{
				List<Vector2D> np = new List<Vector2D>();
				
				//считываем входные точки
				for (int i = 0; i < FXY.SliceCount; i++)
				{
					double valx = FXY[i].x;
					double valy = FXY[i].y;
					valx = VMath.Map(valx, -1, 1, 0, Width, TMapMode.Clamp);
					valy = VMath.Map(valy, -1, 1, 0, Height, TMapMode.Clamp);
					np.Add(new Vector2D(valx, valy));
				}
				if (np.Count != 0)
				{
					FAllLines.NewPoints(np);
				}
			}
			#endregion
		}
		#endregion mainloop

		protected override CustomDeviceData CreateDeviceData(Device device)
		{
			return new CustomDeviceData(device);
		}

		protected override void UpdateDeviceData(CustomDeviceData deviceData)
		{
			Device device = deviceData.Device;

			List<Mesh> meshes = new List<Mesh>();

			foreach (SmoothLine t in FAllLines.BreakedSmoothLines)
			{
				if (t.Indexes.Count == 0 || t.Vertices.Count == 0) continue;

				Mesh mesh = new Mesh(device, t.Indexes.Count / 3,
									 t.Vertices.Count, MeshFlags.Dynamic | MeshFlags.WriteOnly, Vertex.Format);
				DataStream vS = mesh.LockVertexBuffer(LockFlags.Discard);
				DataStream iS = mesh.LockIndexBuffer(LockFlags.Discard);

				vS.WriteRange(t.Vertices.ToArray());
				iS.WriteRange(t.Indexes.ToArray());

				mesh.UnlockVertexBuffer();
				mesh.UnlockIndexBuffer();

				meshes.Add(mesh);
			}

			foreach (SmoothLine smoothLine in FAllLines.ActualSmoothLines)
			{
				if (smoothLine.Indexes.Count == 0 || smoothLine.Vertices.Count == 0) continue;

				Mesh mesh = new Mesh(device, smoothLine.Indexes.Count / 3,
				                     smoothLine.Vertices.Count, MeshFlags.Dynamic | MeshFlags.WriteOnly, Vertex.Format);
				DataStream vS = mesh.LockVertexBuffer(LockFlags.Discard);
				DataStream iS = mesh.LockIndexBuffer(LockFlags.Discard);

				vS.WriteRange(smoothLine.Vertices.ToArray());
				iS.WriteRange(smoothLine.Indexes.ToArray());

				mesh.UnlockVertexBuffer();
				mesh.UnlockIndexBuffer();

				meshes.Add(mesh);
			}
			if (meshes.Count != 0)
			{
				Mesh merge = Mesh.Concatenate(device, meshes.ToArray(), MeshFlags.Use32Bit | MeshFlags.Managed);
				FMeshes.Add(deviceData.Device, merge);
			}

			foreach (Mesh m in meshes)
			{
				m.Dispose();
			}

			//device.Dispose();
		}

		protected override void DestroyDeviceData(CustomDeviceData deviceData, bool onlyUnManaged)
		{
			Device device = deviceData.Device;

			if (!FMeshes.ContainsKey(device)) return;
			
			Mesh m = FMeshes[device];
			FMeshes.Remove(device);
			m.Dispose();
		}
	}
}