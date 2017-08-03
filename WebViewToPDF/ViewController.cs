using CoreGraphics;
using Foundation;
using QuickLook;
using System;
using System.IO;
using UIKit;

namespace WebViewToPDF
{
    public partial class ViewController : UIViewController
    {
        private string html;
        private UIWebView webView;

        public ViewController()
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = UIColor.White;
            Title = "WebViewToPDF";

            NavigationItem.SetRightBarButtonItem(new UIBarButtonItem(new NSString("Export"), UIBarButtonItemStyle.Plain, null), true);
            NavigationItem.RightBarButtonItem.Clicked += ExportButtonClicked;

            html = "<HTML><HEAD><TITLE>WebViewToPDF</TITLE></HEAD><BODY><H1>Hello World!</H1><BODY><HTML>";

            string contentDirectoryPath = Path.Combine(NSBundle.MainBundle.BundlePath, "Content/");

            webView = new UIWebView()
            {
                Frame = View.Frame
            };
            webView.LoadHtmlString(html, new NSUrl(contentDirectoryPath, true));
            View.AddSubview(webView);
        }

        private void ExportButtonClicked(object sender, EventArgs e)
        {
            var fileName = "Test.pdf";
            double height, width;

            //A4 page size.
            width = 595.2;
            height = 841.8;

            var renderer = new UIPrintPageRenderer();

            //EvaluateJavascript can be used but it will ignore anything thats not html.
            renderer.AddPrintFormatter(new UIMarkupTextPrintFormatter(webView.EvaluateJavascript("document.documentElement.outerHTML")), 0);
            //EvaluateJavascript can be replaced with HTML string if you have no need to display the HTML in a UIWebView.
            //renderer.AddPrintFormatter(new UIMarkupTextPrintFormatter(html), 0);

            var paperRect = new CGRect(0, 0, width, height);
            var printableRect = RectangleFExtensions.Inset(paperRect, 0, 0);

            renderer.SetValueForKey(FromObject(paperRect), (NSString)"paperRect");
            renderer.SetValueForKey(FromObject(printableRect), (NSString)"printableRect");

            NSData file = BuildPDFWithRenderer(renderer, paperRect);
            File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName), file.ToArray());
            QuickLookPreview(fileName, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName));
        }

        private NSData BuildPDFWithRenderer(UIPrintPageRenderer renderer, CGRect paperRect)
        {
            NSMutableData pdfData = new NSMutableData();
            UIGraphics.BeginPDFContext(pdfData, paperRect, null);

            renderer.PrepareForDrawingPages(new NSRange(0, renderer.NumberOfPages));

            CGRect bounds = UIGraphics.PDFContextBounds;

            for (int i = 0; i < renderer.NumberOfPages; i++)
            {
                UIGraphics.BeginPDFPage();
                renderer.DrawPage(i, paperRect);
            }
            UIGraphics.EndPDFContent();

            return pdfData;
        }

        private void QuickLookPreview(string fileName, string filePath)
        {
            UIViewController currentController = UIApplication.SharedApplication.KeyWindow.RootViewController;
            while (currentController.PresentedViewController != null)
                currentController = currentController.PresentedViewController;
            UIView currentView = currentController.View;

            QLPreviewController qlPreview = new QLPreviewController();
            QLPreviewItem item = new QLPreviewItemBundles(fileName, filePath);
            qlPreview.DataSource = new PreviewControllersDataSource(item);

            currentController.PresentViewController(qlPreview, true, null);
        }
    }


    class QLPreviewItemBundles : QLPreviewItem
    {
        string _fileName, _filePath;
        public QLPreviewItemBundles(string fileName, string filePath)
        {
            _fileName = fileName;
            _filePath = filePath;
        }

        public override string ItemTitle
        {
            get
            {
                return _fileName;
            }
        }
        public override NSUrl ItemUrl
        {
            get
            {
                var documents = NSBundle.MainBundle.BundlePath;
                var lib = Path.Combine(documents, _filePath);
                var url = NSUrl.FromFilename(lib);
                return url;
            }
        }
    }

    public class PreviewControllersDataSource : QLPreviewControllerDataSource
    {
        private QLPreviewItem _item;

        public PreviewControllersDataSource(QLPreviewItem item)
        {
            _item = item;
        }

        public override nint PreviewItemCount(QLPreviewController controller)
        {
            return 1;
        }

        public override IQLPreviewItem GetPreviewItem(QLPreviewController controller, nint index)
        {
            return _item;
        }
    }
}