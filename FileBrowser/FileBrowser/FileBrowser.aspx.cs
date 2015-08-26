using System;
using System.IO;
using IZ.WebFileManager;
using System.Security.AccessControl;
using System.Web;
using System.Web.UI.WebControls;
using System.Globalization;
using MB.FileBrowser;

namespace MB.FileBrowser
{
    public partial class FileBrowser : System.Web.UI.Page, System.Web.UI.ICallbackEventHandler
    {
        protected AjaxJsonResponse ajaxResponse = new AjaxJsonResponse();
        public string Opener;
        public string ImageFolder { get; set; }
        public string FlashFolder { get; set; }
        public string MediaFolder { get; set; }
        public string FilesFolder { get; set; }
        public bool UseDefaultRoots { get; set; }
        public bool UseCustomRoots { get; set; }
        public bool HideCommands { get; set; }

        // data- attributes for configuration of custom roots
        const string  USE_CUSTOMROOTS = "data-usecustomroots";
        const string  USE_DEFAULTROOTS = "data-usedefaultroots" ;
        const string  ROOTS_NAMES =  "data-roots-names";
        const string  ROOTS_SMALLIMAGES =  "data-roots-smallimages";
        const string  ROOTS_LARGEIMAGES = "data-roots-largeimages";
        const string  ROOTS_FOLDERS =  "data-roots-folders"; 
        const string  ROOTS_IMAGEFOLDER =  "data-roots-imagefolder";
        const string READONLY_HIDECOMMANDS = "data-readonly-hidecommands";


        protected void Page_Load(object sender, EventArgs e)
        {
            ImageFolder = (!String.IsNullOrEmpty(HF_FileBrowserConfig.Attributes["data-imagefolder"]) ?
                    HF_FileBrowserConfig.Attributes["data-imagefolder"] : "images");

            FlashFolder = (!String.IsNullOrEmpty(HF_FileBrowserConfig.Attributes["data-flashfolder"]) ?
                    HF_FileBrowserConfig.Attributes["data-flashfolder"] : "flash");

            MediaFolder = (!String.IsNullOrEmpty(HF_FileBrowserConfig.Attributes["data-mediafolder"]) ?
                    HF_FileBrowserConfig.Attributes["data-mediafolder"] : "media");

            FilesFolder = (!String.IsNullOrEmpty(HF_FileBrowserConfig.Attributes["data-filesfolder"]) ?
                    HF_FileBrowserConfig.Attributes["data-filesfolder"] : "files");

            string useCustomStr = String.IsNullOrEmpty(HF_CustomRoots.Attributes[USE_CUSTOMROOTS]) ? "" :  HF_CustomRoots.Attributes[USE_CUSTOMROOTS];
            string useDefaultStr = String.IsNullOrEmpty(HF_CustomRoots.Attributes[USE_DEFAULTROOTS]) ? "" :  HF_CustomRoots.Attributes[USE_DEFAULTROOTS];
            string hideCommandsStr = String.IsNullOrEmpty(HF_FileBrowserConfig.Attributes[READONLY_HIDECOMMANDS]) ? "" : HF_FileBrowserConfig.Attributes[READONLY_HIDECOMMANDS];

            UseCustomRoots = useCustomStr.ToLower() != "false";
            UseDefaultRoots = useDefaultStr.ToLower() == "true";
            HideCommands = hideCommandsStr != "false";


            //if (Request.Url.Host.IndexOf("localhost") > -1)
            //    FileManager1.DefaultAccessMode = AccessMode.Write;

            CultureInfo culture;
            try
            {
                culture = new CultureInfo(Request["langCode"]);
            }
            catch (Exception)
            {
                culture = CultureInfo.CurrentCulture;
            }
            FileManager1.ShowAddressBar = false;
            FileManager1.AllowUpload = false;

            String cbReference =
                Page.ClientScript.GetCallbackEventReference(this,
                "arg", "ReceiveServerData", "context");
            String callbackScript;
            callbackScript = "function CallServer(arg, context)" +
                "{ " + cbReference + ";}";
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(),
                "CallServer", callbackScript, true);

            if (!IsPostBack)
            {
                /**
                 * -------- Parameters --------------
                 * CKEDITOR automatically call FileManager service adding two custom parameters 
                 * CKEditorFuncNum e type.
                 * First parameter allows you to pass choosen file url back to CKEDITOR 
                 * through callback function
                 * Type paramete is used to restrict the file search to a 
                 * specific folder
                 * 
                 * Tiny MCE 4 use the type parameter and field parameter wich is 
                 * the id of the field whose value must be set.
                 * 
                 * 
                 * Other recognized parameters
                 * caller: 
                 *      allowed values: ckeditor, tinymce, parent, top
                 *      default: caller id defaulted to ckeditor if the CKEditor parameter is specified otherwise to parent
                 *      Idicates the object to wich the callback must be return  
                 *      
                 * fn:
                 *      allowed values: any string
                 *      default: null
                 *      Function name to be called.
                 * 
                 * langCode:
                 *      allowed value: a standard language code
                 *      default: current language
                 *      CKEdito pass this paramenter automatically
                 *      
                 */

                int fnumber = 0;
                string caller, fn;

                // the caller is CKEditor
                if (!string.IsNullOrEmpty(Request["CKEditor"]))
                {
                    caller = "ckeditor";
                }
                else
                    caller = (String.IsNullOrEmpty(Request["caller"]) ? "parent" : Request["caller"]);

                HF_Opener.Value = caller;

                fn = Request["fn"];

                if (!String.IsNullOrEmpty(fn))
                    HF_CallBack.Value = fn;

                if (int.TryParse(Request["CKEditorFuncNum"], out fnumber))
                    HF_CKEditorFunctionNumber.Value = fnumber.ToString();

                if (!String.IsNullOrEmpty(Request["field"]))
                    HF_Field.Value = Request["field"];

                string type = "";
                string mainRoot = "~/userfiles";

                if (FileManager1.Culture == null)
                    FileManager1.Culture = culture;

                HF_CurrentCulture.Value = FileManager1.Culture.Name;

                FileManager1.CustomToolbarButtons[0].Text = FileManager1.Controller.GetResourceString("View_file", "View File");
                Upload_button.InnerText = FileManager1.Controller.GetResourceString("Upload_file_click", "Click here to download a file");
                DND_message.InnerText = FileManager1.Controller.GetResourceString("Upload_dnd", "Or drag 'nd drop one or more files on the above area");

                if (!String.IsNullOrEmpty(FileManager1.MainDirectory))
                    mainRoot = FileManager1.MainDirectory;
                //mainRoot = ResolveClientUrl(mainRoot);
                if (!Directory.Exists(Server.MapPath(ResolveClientUrl(mainRoot))))
                    throw new Exception("User directory with write privileges is needed.");

                DirectoryInfo mainRootInfo = new DirectoryInfo(Server.MapPath(ResolveClientUrl(mainRoot)));

                if (!String.IsNullOrEmpty(Request["type"]))
                {
                    type = Request["type"];
                }

                RootDirectory images, flash, files, media;
                // Display text of root folders are localized using WebFileBrowser resources files
                // in "/App_GlobalResources/WebFileManager" and GetResoueceString method
                // of FileManager.Controller class

                FileManager1.RootDirectories.Clear();
                if (UseDefaultRoots)
	                {

                    mainRootInfo.CreateSubdirectory(ImageFolder);
                    mainRootInfo.CreateSubdirectory(FilesFolder);
                    mainRootInfo.CreateSubdirectory(FlashFolder);
                    mainRootInfo.CreateSubdirectory(MediaFolder);

		            switch (type)
                        {
                        case "images":
                        case "image":
                            FileManager1.RootDirectories.Clear();
                            images = new RootDirectory();
                            images.ShowRootIndex = false;
                            images.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + ImageFolder;
                            images.Text = FileManager1.Controller.GetResourceString("Root_Image", "Images");
                            images.LargeImageUrl = "~/FileBrowser/img/32/camera.png";
                            images.SmallImageUrl = "~/FileBrowser/img/16/camera.png";
                            FileManager1.RootDirectories.Add(images);
                            break;
                        case "flash":
                            FileManager1.RootDirectories.Clear();
                            flash = new RootDirectory();
                            flash.ShowRootIndex = false;
                            flash.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + FlashFolder;
                            flash.LargeImageUrl = "~/FileBrowser/img/32/folder-flash.png";
                            flash.SmallImageUrl = "~/FileBrowser/img/16/folder-flash.png";
                            flash.Text = FileManager1.Controller.GetResourceString("Root_Flash", "Flash Movies");
                            FileManager1.RootDirectories.Add(flash);
                            break;
                        case "files":
                            FileManager1.RootDirectories.Clear();
                            files = new RootDirectory();
                            files.ShowRootIndex = false;
                            files.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + FilesFolder;
                            files.LargeImageUrl = "~/FileBrowser/img/32/folder-document-alt.png";
                            files.SmallImageUrl = "~/FileBrowser/img/16/folder-document-alt.png";
                            files.Text = FileManager1.Controller.GetResourceString("Root_File", "Files");
                            FileManager1.RootDirectories.Add(files);
                            break;
                        case "media":
                            FileManager1.RootDirectories.Clear();
                            media = new RootDirectory();
                            media.ShowRootIndex = false;
                            media.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + MediaFolder;
                            media.LargeImageUrl = "~/FileBrowser/img/32/folder-video-alt.png";
                            media.SmallImageUrl = "~/FileBrowser/img/16/folder-video-alt.png";
                            media.Text = FileManager1.Controller.GetResourceString("Root_Media", "Media");
                            FileManager1.RootDirectories.Add(media);
                            break;
                        default:
                            FileManager1.RootDirectories.Clear();
                            files = new RootDirectory();
                            files.ShowRootIndex = false;
                            files.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + FilesFolder;
                            files.LargeImageUrl = "~/FileBrowser/img/32/folder-document-alt.png";
                            files.SmallImageUrl = "~/FileBrowser/img/16/folder-document-alt.png";
                            // Display text of root folders are localized using WebFileBrowser resources files
                            // in "/App_GlobalResources/WebFileManager" and GetResoueceString method
                            // of FileManager.Controller class
                            files.Text = FileManager1.Controller.GetResourceString("Root_File", "Files");
                            FileManager1.RootDirectories.Add(files);

                            flash = new RootDirectory();
                            flash.ShowRootIndex = false;
                            flash.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + FlashFolder;
                            flash.LargeImageUrl = "~/FileBrowser/img/32/folder-flash.png";
                            flash.SmallImageUrl = "~/FileBrowser/img/16/folder-flash.png";
                            flash.Text = FileManager1.Controller.GetResourceString("Root_Flash", "Flash Movies");
                            FileManager1.RootDirectories.Add(flash);

                            images = new RootDirectory();
                            images.ShowRootIndex = false;
                            images.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + ImageFolder;
                            images.Text = FileManager1.Controller.GetResourceString("Root_Image", "Images");
                            images.LargeImageUrl = "~/FileBrowser/img/32/camera.png";
                            images.SmallImageUrl = "~/FileBrowser/img/16/camera.png";
                            FileManager1.RootDirectories.Add(images);

                            media = new RootDirectory();
                            media.ShowRootIndex = false;
                            media.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + MediaFolder;
                            media.LargeImageUrl = "~/FileBrowser/img/32/folder-video-alt.png";
                            media.SmallImageUrl = "~/FileBrowser/img/16/folder-video-alt.png";
                            media.Text = FileManager1.Controller.GetResourceString("Root_Media", "Media");
                            FileManager1.RootDirectories.Add(media);

                            break;
                    }
                }


                if (UseCustomRoots)
                {
                    // Memorizza il parametro querystring "cs" che consente di visualizzare una sola customroot
                    int selectedCustomRoot;
                    if (!int.TryParse(Request["cs"], out selectedCustomRoot))
                        selectedCustomRoot = -1;

                    // Folder containing custom roots icon images
                    string rootsImageFolder = String.IsNullOrEmpty(HF_CustomRoots.Attributes[ROOTS_IMAGEFOLDER]) ? "" : HF_CustomRoots.Attributes[ROOTS_IMAGEFOLDER];

                    //Arrays: roots names, roots folders, small icons, large icons
                    string[] rootsNames, rootsFolders, rootsSmallImages, rootsLargeImages;

                    // Convert data-roots-names value in array
                    string temp = String.IsNullOrEmpty(HF_CustomRoots.Attributes[ROOTS_NAMES]) ? "" : HF_CustomRoots.Attributes[ROOTS_NAMES];
                    if (temp == "")
                    {
                        return;
                    }
                    rootsNames = temp.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // Count of custom roots
                    int rootsCount = rootsNames.Length;

                    // If data-roots-folder is empty, will use root names
                    if (String.IsNullOrEmpty(HF_CustomRoots.Attributes[ROOTS_FOLDERS]))
                    {
                        rootsFolders = temp.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        rootsFolders = HF_CustomRoots.Attributes[ROOTS_FOLDERS].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }


                    if (rootsCount > rootsFolders.Length)
                        rootsCount = rootsFolders.Length;

                    if (!String.IsNullOrEmpty(HF_CustomRoots.Attributes[ROOTS_SMALLIMAGES]))
                    {
                        rootsSmallImages = HF_CustomRoots.Attributes[ROOTS_SMALLIMAGES].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (rootsCount > rootsSmallImages.Length)
                            rootsCount = rootsSmallImages.Length;
                    }
                    else
                    {
                        rootsSmallImages = new string [] {};
                        rootsCount = 0;

                    }

                    if (!String.IsNullOrEmpty(HF_CustomRoots.Attributes[ROOTS_LARGEIMAGES]))
                    {
                        rootsLargeImages = HF_CustomRoots.Attributes[ROOTS_LARGEIMAGES].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (rootsCount > rootsLargeImages.Length)
                            rootsCount = rootsLargeImages.Length;
                    }
                    else
                    {
                        rootsLargeImages = new string[] { };
                        rootsCount = 0;

                    }

                    if (rootsCount == 0)
                    {
                        throw new Exception("If UseCustomRoots option is setted you must provide Custom Roots full info (Names, Folders, small an large images).");
                    }
                    else
                    {
                        if (selectedCustomRoot >= 0 && selectedCustomRoot < rootsCount)
                        {
                            mainRootInfo.CreateSubdirectory(rootsFolders[selectedCustomRoot]);
                            RootDirectory myCustomRoot = new RootDirectory();
                            myCustomRoot.ShowRootIndex = false;
                            myCustomRoot.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + rootsFolders[selectedCustomRoot];
                            myCustomRoot.LargeImageUrl = VirtualPathUtility.AppendTrailingSlash(rootsImageFolder) + rootsLargeImages[selectedCustomRoot];
                            myCustomRoot.SmallImageUrl = VirtualPathUtility.AppendTrailingSlash(rootsImageFolder) + rootsLargeImages[selectedCustomRoot];
                            myCustomRoot.Text = rootsNames[selectedCustomRoot];
                            FileManager1.RootDirectories.Add(myCustomRoot);
                        }
                        else
                        {
                            for (int i = 0; i < rootsCount; i++)
                            {
                                mainRootInfo.CreateSubdirectory(rootsFolders[i]);
                                RootDirectory myCustomRoot = new RootDirectory();
                                myCustomRoot.ShowRootIndex = false;
                                myCustomRoot.DirectoryPath = VirtualPathUtility.AppendTrailingSlash(mainRoot) + rootsFolders[i];
                                myCustomRoot.LargeImageUrl = VirtualPathUtility.AppendTrailingSlash(rootsImageFolder) + rootsLargeImages[i];
                                myCustomRoot.SmallImageUrl = VirtualPathUtility.AppendTrailingSlash(rootsImageFolder) + rootsLargeImages[i];
                                myCustomRoot.Text = rootsNames[i];
                                FileManager1.RootDirectories.Add(myCustomRoot);
                            }

                        }
                    } 
	            }            
            }

            AccessMode fbAccessMode;
            if (MagicSession.Current.FileBrowserAccessMode == AccessMode.Default)
                fbAccessMode = FileManager1.DefaultAccessMode;
            else
                fbAccessMode = MagicSession.Current.FileBrowserAccessMode;

            Literal content;

            switch (fbAccessMode)
            {
                case AccessMode.Delete:
                    FileManager1.Visible = true;
                    Panel_upload.Visible = true;
                    Panel_deny.Visible = false;
                    FileManager1.ReadOnly = false;
                    FileManager1.AllowDelete = true;
                    FileManager1.AllowOverwrite = true;
                    break;
                case AccessMode.DenyAll:
                    FileManager1.Visible = false;
                    Panel_upload.Visible = false;
                    Panel_deny.Visible = true;
                    content = new Literal();
                    content.Text = "<h1>" + FileManager1.Controller.GetResourceString("Upload_Error_3", "User does not have sufficient privileges.") + "<br/>&nbsp;</h1>";
                    Panel_deny.Controls.Add(content);
                    break;
                case AccessMode.ReadOnly:
                case AccessMode.Default:
                    FileManager1.Visible = true;
                    Panel_upload.Visible = false;
                    Panel_deny.Visible = false;
                    FileManager1.ReadOnly = true;
                    if (HideCommands)
                    {
                        FileManager1.ShowToolBar = false;
                        FileManager1.EnableContextMenu = false;
                        Panel_upload.Visible = true;
                        Upload_button.Visible = false;
                        DND_message.InnerText = FileManager1.Controller.GetResourceString("No_Command_Help", "DoubleClick to open a folder. DoubleClick to download a file.");
                    }
                    break;
                case AccessMode.Write:
                    FileManager1.Visible = true;
                    Panel_upload.Visible = true;
                    Panel_deny.Visible = false;
                    FileManager1.ReadOnly = false;
                    FileManager1.AllowDelete = false;
                    FileManager1.AllowOverwrite = false;
                    break;
                default:
                    break;
            }


        }
        public void RaiseCallbackEvent(String eventArgument)
        {
            string[] cmds = eventArgument.Split(new char[] { ',' });
            ajaxResponse.command = cmds[0].ToLower();
            switch (cmds[0].ToLower())
            {
                case "showfile":
                    if (FileManager1.CurrentDirectory != null)
                        ajaxResponse.data = VirtualPathUtility.AppendTrailingSlash(FileManager1.CurrentDirectory.VirtualPath) + cmds[1];
                    break;
                case "upload":
                    if (FileManager1.CurrentDirectory != null)
                        ajaxResponse.data = VirtualPathUtility.AppendTrailingSlash(FileManager1.CurrentDirectory.VirtualPath);
                    break;
                default:
                    break;
            }

        }
        public String GetCallbackResult()
        {
            System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            return serializer.Serialize(ajaxResponse);

        }

    }
}