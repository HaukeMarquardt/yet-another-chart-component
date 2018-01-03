﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region Background
	/// <summary>
	/// Background fill for the chart data area.
	/// </summary>
	public class Background : ChartComponent, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		#region properties
		/// <summary>
		/// The fill brush to use.
		/// </summary>
		public Brush Fill { get { return (Brush)GetValue(FillProperty); } set { SetValue(FillProperty, value); } }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path Path { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected RectangleGeometry Rectangle { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Fill"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(Background), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public Background() {
			Rectangle = new RectangleGeometry();
			Path = new Path() {
				Data = Rectangle
			};
		}
		#endregion
		#region helpers
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, "Fill", Path, Path.FillProperty);
			#if false
			BindTo(this, "GridStroke", Grid, Path.StrokeProperty);
			BindTo(this, "GridStrokeThickness", Grid, Path.StrokeThicknessProperty);
			var bx = GetBindingExpression(GridVisibilityProperty);
			if (bx != null) {
				Grid.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "GridVisibility", Grid, Path.VisibilityProperty);
			}
			#endif
		}
		#endregion
		#region extensions
		/// <summary>
		/// Component is entering the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		public void Enter(IChartEnterLeaveContext icelc) {
			icelc.Add(Path);
			DoBindings(icelc);
		}
		/// <summary>
		/// Component is leaving the chart.
		/// </summary>
		/// <param name="icelc">Context.</param>
		public void Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(Path);
		}
		/// <summary>
		/// Render the background.
		/// Uses NDC coordinates.
		/// </summary>
		/// <param name="icrc">Context.</param>
		public void Render(IChartRenderContext icrc) {
			//if (!Dirty) return;
			Rectangle.Rect = new Windows.Foundation.Rect(0, 0, 1, 1);
		}
		/// <summary>
		/// Scale the NDC rectangle to the dimensions given.
		/// </summary>
		/// <param name="icrc">Context.</param>
		public void Transforms(IChartRenderContext icrc) {
			var matx = new Matrix(icrc.SeriesArea.Width, 0, 0, icrc.SeriesArea.Height, icrc.SeriesArea.Left, icrc.SeriesArea.Top);
			Rectangle.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
	#region HorizontalRule
	/// <summary>
	/// Represents a horizontal "rule" on the chart.
	/// </summary>
	public class HorizontalRule : ChartComponent, IProvideValueExtents, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		#region properties
		/// <summary>
		/// Brush for the axis grid lines.
		/// </summary>
		public Brush Stroke { get { return (Brush)GetValue(StrokeProperty); } set { SetValue(StrokeProperty, value); } }
		/// <summary>
		/// Stroke thickness for rule.
		/// Default value is 1
		/// </summary>
		public double StrokeThickness { get; set; } = 1;
		/// <summary>
		/// Visibility of the grid lines.
		/// </summary>
		public Visibility RuleVisibility { get { return (Visibility)GetValue(RuleVisibilityProperty); } set { SetValue(RuleVisibilityProperty, value); } }
		/// <summary>
		/// Component name of value axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String ValueAxisName { get; set; }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public double Value { get { return (double)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
		/// <summary>
		/// Whether to clip geometry to the data region.
		/// When true, rule will NEVER display outside the data region.
		/// Default value is true.
		/// </summary>
		public bool ClipToDataRegion { get; set; } = true;
		/// <summary>
		/// Whether to expose the value to the value axis.
		/// When true, forces this rule's value to appear on the axis.
		/// Default value is True.
		/// </summary>
		public bool ShowOnAxis { get; set; } = true;
		/// <summary>
		/// Property for IProvideValueExtents.
		/// </summary>
		public double Minimum { get { return Value; } }
		/// <summary>
		/// Property for IProvideValueExtents.
		/// </summary>
		public double Maximum { get { return Value; } }
		/// <summary>
		/// The path to attach geometry et al.
		/// </summary>
		protected Path Path { get; set; }
		/// <summary>
		/// The geometry to use for this component.
		/// </summary>
		protected LineGeometry Rule { get; set; }
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Stroke"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(HorizontalRule), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="RuleVisibility"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty RuleVisibilityProperty = DependencyProperty.Register("RuleVisibility", typeof(Visibility), typeof(HorizontalRule), new PropertyMetadata(null));
		/// <summary>
		/// Value DP.
		/// </summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value", typeof(double), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(ComponentPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Calls DataSeries.ProcessData().
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		private static void ComponentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			HorizontalRule hr = d as HorizontalRule;
			hr.Dirty = true;
			hr.Refresh();
		}
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public HorizontalRule() {
			Rule = new LineGeometry();
			Path = new Path() {
				Data = Rule
			};
		}
		#endregion
		#region helpers
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, "Stroke", Path, Path.FillProperty);
			BindTo(this, "Stroke", Path, Path.StrokeProperty);
			BindTo(this, "StrokeThickness", Path, Path.StrokeThicknessProperty);
			var bx = GetBindingExpression(RuleVisibilityProperty);
			if (bx != null) {
				Path.SetBinding(UIElement.VisibilityProperty, bx.ParentBinding);
			} else {
				BindTo(this, "RuleVisibility", Path, Path.VisibilityProperty);
			}
		}
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartRenderContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			}
		}
		#endregion
		#region extensions
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc"></param>
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			//_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis}");
			icelc.Add(Path);
			DoBindings(icelc);
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(Path);
		}
		/// <summary>
		/// Rule coordinates:
		///		x: "normalized" [0..1] and scaled to the area-width
		///		y: "axis" scale
		/// </summary>
		/// <param name="icrc"></param>
		public void Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			if (ValueAxis == null) return;
			//_trace.Verbose($"{Name} val:{Value}");
			var vx = ValueAxis.For(Value);
			Rule.StartPoint = new Point(0, vx);
			Rule.EndPoint = new Point(1, vx);
			if(ClipToDataRegion) {
				Path.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
			if (ShowOnAxis) {
				ValueAxis.UpdateLimits(Value);
			}
			Dirty = false;
		}
		/// <summary>
		/// rule coordinates (x:[0..1], y:axis)
		/// </summary>
		/// <param name="icrc"></param>
		public void Transforms(IChartRenderContext icrc) {
			var gscaley = icrc.SeriesArea.Height / ValueAxis.Range;
			var gmatx = new Matrix(icrc.SeriesArea.Width, 0, 0, -gscaley, icrc.SeriesArea.Left, icrc.SeriesArea.Top + ValueAxis.Maximum * gscaley);
			//_trace.Verbose($"transforms sy:{scaley:F3} gsy:{gscaley:F3} matx:{matx} gmatx:{gmatx} a:{icrc.Area} sa:{icrc.SeriesArea}");
			Rule.Transform = new MatrixTransform() { Matrix = gmatx };
		}
		#endregion
	}
	#endregion
}
