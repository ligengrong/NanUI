﻿using Chromium.WebBrowser;
using NetDimension.Windows.Imports;
using NetDimension.WinForm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Chromium;
using System.ComponentModel;
using Chromium.Remote;
using Chromium.WebBrowser.Event;

namespace NetDimension.NanUI
{
	public class Formium : ModernUIForm, IChromiumClient
	{
		private readonly WebBrowserControl BrowserWrapper;
		private readonly Panel splashPanel;
		private bool isFirstTimeShowSplash;
		private Dictionary<string, string> delayedScripts = new Dictionary<string, string>();


		protected IntPtr FormHandle { get; private set; }


		protected BrowserCore Chromium => BrowserWrapper?.Chromium;

		protected IntPtr BrowserHandle
		{
			get
			{
				return Chromium.BrowserHost.WindowHandle;
			}
		}

		#region ICefClient
		[Browsable(false)]
		public CfxBrowser Browser => Chromium?.Browser;
		[Browsable(false)]
		public CfxBrowserHost BrowserHost => Chromium.BrowserHost;
		[Browsable(false)]
		public Uri Url => Chromium.Url;
		[Browsable(false)]
		public bool IsLoading => Chromium.IsLoading;
		[Browsable(false)]
		public bool CanGoBack => Chromium.CanGoBack;
		[Browsable(false)]
		public bool CanGoForward => Chromium.CanGoForward;
		[Browsable(false)]
		public JSObject GlobalObject => Chromium.GlobalObject;
		[Browsable(false)]
		public CfxContextMenuHandler ContextMenuHandler => Chromium.ContextMenuHandler;
		[Browsable(false)]
		public CfxLifeSpanHandler LifeSpanHandler => Chromium.LifeSpanHandler;
		[Browsable(false)]
		public CfxLoadHandler LoadHandler => Chromium.LoadHandler;
		[Browsable(false)]
		public CfxRequestHandler RequestHandler => Chromium.RequestHandler;
		[Browsable(false)]
		public CfxDisplayHandler DisplayHandler => Chromium.DisplayHandler;
		[Browsable(false)]
		public CfxDownloadHandler DownloadHandler => Chromium.DownloadHandler;
		[Browsable(false)]
		public CfxDragHandler DragHandler => Chromium.DragHandler;
		[Browsable(false)]
		public CfxDialogHandler DialogHandler => Chromium.DialogHandler;
		[Browsable(false)]
		public CfxFindHandler FindHandler => Chromium.FindHandler;
		[Browsable(false)]
		public CfxFocusHandler FocusHandler => Chromium.FocusHandler;
		[Browsable(false)]
		public CfxGeolocationHandler GeolocationHandler => Chromium.GeolocationHandler;
		[Browsable(false)]
		public CfxJsDialogHandler JsDialogHandler => Chromium.JsDialogHandler;
		[Browsable(false)]
		public CfxKeyboardHandler KeyboardHandler => Chromium.KeyboardHandler;

		public void GoBack() => Chromium.GoBack();
		public void GoForward() => Chromium.GoForward();
		public void LoadUrl(string url) => Chromium.LoadUrl(url);
		public void LoadString(string stringVal, string url) => Chromium.LoadString(stringVal, url);
		public void LoadString(string stringVal) => Chromium.LoadString(stringVal);
		public int Find(string searchText, bool forward, bool matchCase) => Chromium.Find(searchText, forward, matchCase);
		public int Find(string searchText, bool forward) => Chromium.Find(searchText, forward);
		public int Find(string searchText) => Chromium.Find(searchText);
		public bool ExecuteJavascript(string code) => Chromium.ExecuteJavascript(code);
		public bool ExecuteJavascript(string code, string scriptUrl, int startLine) => Chromium.ExecuteJavascript(code, scriptUrl, startLine);
		public bool EvaluateJavascript(string code, Action<CfrV8Value, CfrV8Exception> callback) => Chromium.EvaluateJavascript(code, callback);
		public bool EvaluateJavascript(string code, JSInvokeMode invokeMode, Action<CfrV8Value, CfrV8Exception> callback) => Chromium.EvaluateJavascript(code, invokeMode, callback);
		public JSObject GlobalObjectForFrame(string frameName) => Chromium.GlobalObjectForFrame(frameName);
		#endregion
		[Browsable(false)]
		public Panel SplashPanel
		{
			get
			{
				return splashPanel;
			}
		}

		/// <summary>
		/// 设置或获取NanUI窗口加载等待画面使用的图片
		/// </summary>
		[Category("NanUI")]
		public Image SplashImage
		{
			get
			{
				return splashPanel.BackgroundImage;
			}
			set
			{
				splashPanel.BackgroundImage = value;
			}
		}
		/// <summary>
		/// 设置或获取NanUI窗口加载等待画面图片布局方式
		/// </summary>
		[Category("NanUI")]
		public ImageLayout SplashImageLayout
		{
			get
			{
				return splashPanel.BackgroundImageLayout;
			}
			set
			{
				splashPanel.BackgroundImageLayout = value;
			}
		}


		/// <summary>
		/// 设置或获取NanUI窗口加载等待画面背景颜色
		/// </summary>
		[Category("NanUI")]
		public Color SplashBackColor
		{
			get
			{
				return splashPanel.BackColor;
			}
			set
			{
				splashPanel.BackColor = value;
			}
		}



		public Formium()
				: this(null)
		{

		}

		//public Formium(string initialUrl)
		//		: this(initialUrl, true)
		//{

		//}


		public Formium(string initialUrl)
				: base(true)
		{
			FormHandle = this.Handle;


			this.DoubleBuffered = true;

			UpdateStyles();




			splashPanel = new Panel()
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent
			};



			if (!IsDesignMode)
			{
				BrowserWrapper = new WebBrowserControl(initialUrl);
				Controls.Add(BrowserWrapper);
				BrowserWrapper.Dock = DockStyle.Fill;
				Chromium.OnBrowserMessage += WebBrowserCore_OnBrowserMessage;
				Chromium.RemoteBrowserCreated += WebBrowserCore_RemoteBrowserCreated;
				BrowserWrapper.SendToBack();







				LoadHandler.OnLoadEnd += (_, args) =>
				{
					if (args.Frame.IsMain)
					{
						foreach (var script in delayedScripts)
						{
							ExecuteJavascript(script.Value);
						}

						HideInitialSplash();
					}
				};



				this.Controls.Add(splashPanel);
				splashPanel.BringToFront();

				isFirstTimeShowSplash = true;


			}

		}


		private FormV8Handler formV8Handler;

		private void WebBrowserCore_RemoteBrowserCreated(object sender, RemoteBrowserCreatedEventArgs e)
		{
			formV8Handler = new FormV8Handler(this);
#if XP
			CfrRuntime.RegisterExtension("nanui/form", NetDimension.NanUI.XP.Properties.Resources.nanui_formExtension, formV8Handler);
#else
			CfrRuntime.RegisterExtension("nanui/form", NetDimension.NanUI.Properties.Resources.nanui_formExtension, formV8Handler);
#endif


		}


		protected override void OnHandleCreated(EventArgs e)
		{
			FormHandle = this.Handle;

			base.OnHandleCreated(e);

		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			var js = "raiseCustomEvent('hostactivestate', {state:1, stateName:'activated'})";


			if (Chromium  == null || !Chromium.IsMainFrameLoaded || !ExecuteJavascript(js))
			{
				delayedScripts["hostactivestate"] = js;
			}
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);

			var js = "raiseCustomEvent('hostactivestate', {state:0, stateName:'deactivated'})";


			if (Chromium == null || !Chromium.IsMainFrameLoaded || !ExecuteJavascript(js))
			{
				delayedScripts["hostactivestate"] = js;
			}
			else
			{
				Browser.Host.NotifyMoveOrResizeStarted();
			}
		}

		private int? lastWindowState = null;

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);



			var currentState = 0;
			var stateString = "normal";

			if (WindowState == FormWindowState.Maximized)
			{
				currentState = 2;
				stateString = "maximized";

			}
			else if (WindowState == FormWindowState.Minimized)
			{

				currentState = 1;
				stateString = "minimized";
			}


			if(lastWindowState!= currentState)
			{
				var rect = new RECT();
				User32.GetClientRect(Handle, ref rect);


				var js = $"raiseCustomEvent('hoststatechange', " +
						$"{{" +
						$"state: {currentState}," +
						$"stateName: \"{stateString}\"," +
						$"width: {rect.Width}," +
						$"height: {rect.Height}" +
						$"}});";

				if (Chromium == null || !Chromium.IsMainFrameLoaded || !ExecuteJavascript(js))
				{
					delayedScripts["hoststatechange"] = js;
				}
				else
				{
					Browser.Host.NotifyMoveOrResizeStarted();
				}

				lastWindowState = currentState;
			}



		}

		protected override void OnMove(EventArgs e)
		{
			if (Browser != null )
			{
				Browser.Host.NotifyMoveOrResizeStarted();
			}

			base.OnMove(e);
		}



		protected void HideInitialSplash()
		{
			if (isFirstTimeShowSplash)
			{
				HideSplash();
				isFirstTimeShowSplash = false;
			}
		}

		protected void HideSplash()
		{
			this.RequireUIThread(() =>
			{

				splashPanel.Visible = false;
				splashPanel.SendToBack();
				Chromium.SetToTop();
			});
		}

		private POINT GetPostionFromPtr(IntPtr lparam)
		{
			var scaledX = (int)User32.LoWord(lparam);
			var scaledY = (int)User32.HiWord(lparam);

			var x = scaledX;
			var y = scaledY;

			return new POINT(x, y);
		}
		private void WebBrowserCore_OnBrowserMessage(object sender, Chromium.WebBrowser.BrowserMessageEventArgs e)
		{
			if (BrowserHandle == IntPtr.Zero) return;

			var msg = (WindowsMessages)e.BrowserMessage.Msg;
			if (CanResize && msg == WindowsMessages.WM_MOUSEMOVE)
			{
				var pt = GetPostionFromPtr(e.BrowserMessage.LParam);
				var mode = GetSizeMode(pt);

				if (mode != HitTest.HTCLIENT)
				{
					User32.ClientToScreen(FormHandle, ref pt);
					User32.PostMessage(FormHandle, (uint)WindowsMessages.WM_NCHITTEST, IntPtr.Zero, Win32.MakeParam((IntPtr)pt.x, (IntPtr)pt.y));
					e.Handled = true;
				}
			}

			if (msg == WindowsMessages.WM_LBUTTONDOWN)
			{
				var pt = GetPostionFromPtr(e.BrowserMessage.LParam);
				var dragable = (Chromium.DraggableRegion != null && Chromium.DraggableRegion.IsVisible(new Point(pt.x, pt.y)));

				var mode = GetSizeMode(pt);
				if (CanResize && mode != HitTest.HTCLIENT)
				{

					User32.ClientToScreen(FormHandle, ref pt);

					User32.PostMessage(FormHandle, (uint)WindowsMessages.WM_NCLBUTTONDOWN, (IntPtr)mode, Win32.MakeParam((IntPtr)pt.x, (IntPtr)pt.y));

					e.Handled = true;

				}
				else if (dragable && !(FormBorderStyle == FormBorderStyle.None && WindowState == FormWindowState.Maximized))
				{
					Browser.Host.NotifyMoveOrResizeStarted();

					User32.PostMessage(FormHandle, (uint)DefMessages.WM_NANUI_DRAG_APP_REGION, IntPtr.Zero, IntPtr.Zero);

					e.Handled = true;

				}
			}

			if (CanResize && msg == WindowsMessages.WM_LBUTTONDBLCLK)
			{
				var pt = GetPostionFromPtr(e.BrowserMessage.LParam);
				var dragable = (Chromium.DraggableRegion != null && Chromium.DraggableRegion.IsVisible(new Point(pt.x, pt.y)));
				if (dragable)
				{
					User32.SendMessage(FormHandle, (uint)WindowsMessages.WM_NCLBUTTONDBLCLK, (IntPtr)HitTest.HTCAPTION, Win32.MakeParam((IntPtr)pt.x, (IntPtr)pt.y));
					e.Handled = true;
				}
			}

			if (msg == WindowsMessages.WM_RBUTTONDOWN)
			{
				var pt = GetPostionFromPtr(e.BrowserMessage.LParam);
				var dragable = (Chromium.DraggableRegion != null && Chromium.DraggableRegion.IsVisible(new Point(pt.x, pt.y)));
				if (dragable)
				{

					User32.SendMessage(FormHandle, (uint)DefMessages.WM_NANUI_APP_REGION_RBUTTONDOWN, IntPtr.Zero, Win32.MakeParam((IntPtr)pt.x, (IntPtr)pt.y));
					e.Handled = true;

				}
			}
		}

		protected override void DefWndProc(ref Message m)
		{

			if (m.Msg == (int)DefMessages.WM_NANUI_DRAG_APP_REGION)
			{

				User32.ReleaseCapture();
				User32.SendMessage(Handle, (uint)WindowsMessages.WM_NCLBUTTONDOWN, (IntPtr)HitTest.HTCAPTION, (IntPtr)0);
			}

			if (m.Msg == (int)DefMessages.WM_NANUI_APP_REGION_RBUTTONDOWN)
			{
				var pt = GetPostionFromPtr(m.LParam);

				var ptToScr = PointToScreen(new Point(pt.x, pt.y));

				ShowSystemMenu(this, ptToScr);
			}

			base.DefWndProc(ref m);
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{


			base.OnFormClosed(e);


			Chromium.Dispose();

		}

		protected override void Dispose(bool disposing)
		{
			Chromium.Dispose();

			base.Dispose(disposing);
		}


	}
}
