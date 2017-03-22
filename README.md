# MB FileBrowser #

MB FileBrowser offer an open source solution to provide server side file browsing to popular html on-line editors (like CKEditor and Tiny MCE) or to your own forms.

The project use a [modified version of IZ File Manager](https://github.com/magicbruno/IZWebFileManager) a server side asp.net web control developed by [manishma](https://github.com/manishma). Refer to [IZWebFileManager](https://github.com/magicbruno/IZWebFileManager) repository for full source code of the control.

The intent of MB File Browser is to offer an easy-to-use ready solution.

## Installation ##
The easiest way to install MB FileBrowser is to use Nuget. 
From within Visual Studio open the Package Manager Console  run the following command:

PM> Install-Package MB.FileBrowser 

This will install last version of the package in your project. 

Next step is to create the root folder for you browser. For default root folder path is **/userfiles**. You may change it setting MainDirectoriy attribute of FileManager1 in FileBrowser.aspx.

**!!! IMPORTANT !!!** Internet anomynous user (IUSR_**) MUST have full control over this folder.

## Use ##
Now MB File Browser is ready to use. Simply open /FileBrowser/FileBrowser.aspx (optionally adding parameters in the query string).

In the [Test Site](http://filemanager.sisteminterattivi.org/) I proposed three examples of how to use File Browser:

The first is a stand alone FileBrowser which allow to choose a file url, or to navigate and download file in a public or private directory: the best way is to place FileBrowser.aspx into a new browser window (with javascript method window.open) or, using an **iframe**, into a javascript dialog (like Bootstrap modal, Fancybox or other). Both example are provided.

The second and the third show how to use MB FileBrowser with CKEditor and Tiny MCE 4. 

For details refer to [Test Site](http://filemanager.sisteminterattivi.org/).

## Configure ##
You have two ways to costomize the MB FileBrowser behavior:

1. Passing proper parameters via querystring when opening FileBrowser.aspx
2. Changing values of the attributes of Filemanager1 control in FileBrowser.aspx.

**Recognised parameters**

- **caller**: "ckeditor" when used with ckeditor, "tinymce4" when used with Tiny MCE, "top" or "parent" when used as stand alone. Indicate the object or the window that called FileBrowser.aspx.

- **fn**: (used only in stand alone mode) name of the function to callBack when user select a file. The callback function will carry a single argument: the url of chosen file.

- **langCode**: this parameter (passed automatically by CKEditor) allow you to customize language of MB FileBrowser.

**Filemanager1 attributes**

In addition to standard properties my version of IZWebFileManager contains some new ones specially developed for MB FileBrowser. You can assign values to IZWebFileManager properties modifing values of attributes of Filemanager1 control in FileBrowser.aspx.

Most of the attributes have self explained names. Here are the most rilevant:

- **MainDirectory**: (new in my version). It's the path of root directory for MB FileBrowser navigation. Internet Anonimous User MUST have full control (read, write, delete/overwrite) over this folder. At the first run,  MB FileBrowser create automatically four subdir (*files*, *images*, *flash* and *media*) where you may organize user files. Folders are created to accomplish the rules that both CkEditor that Tiny MCE follow in file organization.

- **CustomThumbnailHandler**: (new in my version). Url of the ThumbnailHandler. MB FileBrowser provides a default thumbnail handler (/FileBrowser/IZWebFileManagerThumbnailHandler.ashx), but you may write your own. Filemanager call the Thumbnail Handler passing url-ecoded image file url as querystring (ex: MyThumbnailHandler.ashx?%2DirName%2imagename.jpg).  

- **DefaultAccessMode**: (new in my version). It is highly recommended to grant access to the server folders only to registered users. Probably you will define access mode during  the process of authenticating users (see below). This property determines what kind of access is granted to guest users (or when session expires). Default value is *ReadOnly*.

- **ImagesFolder**: It is the folder where Filemanager search image for standard tollbar buttons, pop-up menu items, generic file and folders icons. Yuo may customize icons creating images with same names.

- **FileTypes**: This structure allow to define custom small and large icons for  file types. For undefined file types the generic file icon will be used.

**Note**: you may not use **ShowAddressBar**, **AllowUpload**, **ClientOpenItemFunction**, **AllowOverwrite**, **AllowDelete**, **ReadOnly**. This properties are handled internally by MB FileBrowser.

## Upload ##
MB FileBrowser doesn't use IZWebFileManager upload engine, but its own based on popular FineUploader jQuery plugin with drag'n drop functionality (if browser support it). Simply drag one or more files on fileview area and file(s) will be uploades into current directory. 

To deny upload you mast set FileBrowserAccesMode session property (see below) to *ReadOnly* or to *DenyAll*.

For default only **jpg,jpeg,doc,docx,zip,gif,png,pdf,rar,svg,svgz,xls,xlsx,ppt,pps,pptx** files are accepted. This setting is suitable for most application, anyway you can costomize allowed file types list setting the **AllowedFileTypes** session property.

## Granting access ##
It is highly recommended to grant access to the server folders only to registered users. Probably you will:

1. Define default access for  guest users (or when session expires).
2. Define granted access during  the process of authenticating users.

Default Acces mode is defined by the IZWebFileManager property **DefaultAccessMode**.

When user log in you can grant him proper access setting **FileBrowserAccessMode** session property.

MB FileBrowser offer a typed interface to application session. Through class MagicSession defined in MB.FileBrowser you che handle session properties related to MB_FileBrowser. 

**MB.FileBrowser.MagicSession.Current.FileBrowserAccessMode** define granted access to MB FileBrowser. Valid value are:

- **DenyAll**: no access is allowed. Try to open FileBrowser cause e "Insufficient user prerogative" warning.
- **ReadOnly**: user can only browse and download files.
- **Write**: user can copy and upload  but not overwrite, move or delete files.
- **Delete**: user has full control over files.

**MB.FileBrowser.MagicSession.Current.AllowedFileTypes** define file tipes accepted by upload process. Valid value is a string formatted as a comma separated list of extension without leading dot.

## License ##
MB File Browser is delivered under the free software/open source GNU General Public License (commonly known as the "GPL"). Form more informaztion about  IZWebFileManager license model refer to [project page](http://www.izwebfilemanager.com/).
