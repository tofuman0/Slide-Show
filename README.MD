# Slide Show 
## About
A utility used to set windows wallpaper slideshow and lock screen slideshow. This isn't currently possible by group policy so using this utility will load settings from registry and set slideshow to images in a folder. Just will need to add this utility to either start up folder or create scheduled task that runs on user login. All that can be deployed by group policy.
## Prerequisites
- Window 10 SDK 10.0.19041
- Universal Windows Platform development

## Usage
Run with no arguments. Returns 1 on error. Will continue to run to update the image on the lockscreen but if lock screen slideshow is disabled it'll return 0 on success.
## Registry Values
### HKEY_CURRENT_USER\Control Panel\Desktop
#### SlideShowPath
 **Default Value:** C:\Windows\Wallpaper\SlideShow  
 **Type:** String  
The location of the images to use for the wallpaper slideshow.  
#### SlideShowTicks
 **Default Value:** 1800000  
 **Type:** DWORD (32bit)  
The ticks to wait before changing image in milliseconds  
#### SlideShowShuffle
 **Default Value:** 0  
 **Type:** DWORD (32bit)  
To shuffle the images set to 1  
#### SlideShowEnable
 **Default Value:** 1  
 **Type:** DWORD (32bit)  
Set to 0 to disable wallpaper feature  
### HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Lock Screen
#### LockScreenSlideShowPath
 **Default Value:** C:\Windows\Wallpaper\LockScreen  
 **Type:** String  
The location of the images to use for the lockscreen slideshow.  
#### LockScreenSlideShowTicks
 **Default Value:** 30000  
 **DType:** DWORD (32bit)  
The ticks to wait before changing image in milliseconds  
#### LockScreenSlideShowEnable
 **Default Value:** 1  
 **Type:** DWORD (32bit)  
Set to 0 to disable lockscreen feature  