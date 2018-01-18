﻿using eScape.Core;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region LineSeries
	/// <summary>
	/// Data series that generates a Polyline path.
	/// </summary>
	public class LineSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// The title for the series.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Offset in Category axis offset in [0..1].
		/// Use with ColumnSeries to get the "points" to align with the column(s) layout in their cells.
		/// </summary>
		public double CategoryAxisOffset { get; set; }
		/// <summary>
		/// The series drawing attributes etc. on the Canvas.
		/// </summary>
		protected Path Segments { get; set; }
		/// <summary>
		/// The series geometry.
		/// </summary>
		protected PathGeometry Geometry { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(LineSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(String), typeof(LineSeries), new PropertyMetadata("Title"));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public LineSeries() {
			Geometry = new PathGeometry();
			Segments = new Path() {
				Data = Geometry
			};
		}
		#endregion
		#region extensions
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			Layer = icelc.CreateLayer(Segments);
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
			if (PathStyle == null && Theme != null) {
				if (Theme.PathLineSeries != null) PathStyle = Theme.PathLineSeries;
			}
			BindTo(this, "PathStyle", Segments, Path.StyleProperty);
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public void Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
			if (ClipToDataRegion) {
				Segments.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
		}
		#endregion
		#region IProvideLegend
		private Legend _legend;
		Legend IProvideLegend.Legend {
			get { if (_legend == null) _legend = Legend(); return _legend; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = Segments.Stroke, Stroke = Segments.Stroke };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
			internal PathFigure pf;
			internal bool first = true;
			internal int ix;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by = by,
				pf = new PathFigure()
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			// TODO handle datetime et al values that aren't double
			var valuey = (double)st.by.For(item);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			valuex += CategoryAxisOffset;
			st.ix = index;
			UpdateLimits(valuex, valuey);
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				if(st.bl != null) {
					// still map the X
					CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
				}
				return;
			}
			var mappedy = ValueAxis.For(valuey);
			var mappedx = st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
			_trace.Verbose($"{Name}[{index}] v:({valuex},{valuey}) m:({mappedx},{mappedy})");
			if (st.first) {
				st.pf.StartPoint = new Point(mappedx, mappedy);
				st.first = false;
			} else {
				st.pf.Segments.Add(new LineSegment() { Point = new Point(mappedx, mappedy) });
			}
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			if (st.bx == null) {
				// needs one extra "cell"
				UpdateLimits(st.ix + 1, double.NaN);
			}
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			Geometry.Figures.Clear();
			if (st.pf.Segments.Count > 0) {
				Geometry.Figures.Add(st.pf);
			}
			Dirty = false;
		}
		#endregion
	}
	#endregion
}