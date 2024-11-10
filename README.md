
# ImageRate

<img align="right" src="/Assets/StoreLogo.png">

"ImageRate" is a user-friendly Windows app designed for rating and organizing pictures. With its simple and intuitive interface, users can easily assign rating stars to their images, helping them categorize and prioritize their photo collection. This small yet effective tool is perfect for individuals who want to manage and curate their photos with ease.

## Download
<center>
	<a href="https://apps.microsoft.com/detail/ImageRate/9NZ1B660K8MC?launch=true&mode=mini">
		<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
	</a>
</center>


## Usage hints

When using this program, it will add "rating" metadata tags to your images. To access the rating via the windows file explorer, just can just group or sort the folder by rating.

I'm personally using this program to rate images, and I've found a workflow that works quite well for me. To start off, every image begins with zero stars by default. I go through them once, giving each one at least the first star if it's not completely bad. Then, I revisit the images, adding a star to the ones I like, and I gradually raise my standards with each round. I keep repeating this process until I've narrowed down the number of images to a manageable size. It's a pretty effective method, and I'd recommend giving it a try!

### Keybord shortcuts

To make working with app even more efficient, we added several keyboard shortcuts:
-  `0`-`5` - set rating to specific value
- `+` - increase rating by one
- `-` - degrease rating by one
- arrow `left`/`right` - navigate between images
- `L`/`P` - switch between list and picture view mode
- `F11` - launch fullscreen ("diashow") mode
- `Esc` - exit fullscreen mode

### How to access the assigned rating outside ImageRate

|![img](/Doc/tutorial_image_1.jpg)|![img](/Doc/tutorial_image_2.jpg)|
|-|-|
|Image rating tags are supported in many applications. Inside Windows Explorer, you can sort and group images by rating. Just right-click in an empty space and select the "Rating" option. If "Rating" is not provided as an option, you can add it easily. (See the next step)|If "rating" is not provided as an option, select "More..." at the end of the list. You will see a dialog with a long list providing all kinds of special tags. Scroll down until you find "Rating" and make sure it is checked. Exit the dialog by pressing "OK".|

### Ideas & bug reports

If you miss any specific feature, feel free to ask in the [issue tracker](https://github.com/fm-sys/ImageRate/issues) and we’ll see what we can do…

## Development notes

The project is written in C# using the WinUI 3 framework. It is built with Visual Studio 2022.

To release a new version, build the solution in the `Release` configuration via `Project > Publish > Create App Packages`. You can set the new version code in the `Create App Packages` wizard. Then upload the resulting `.appx` package.

A selfsigned certificate is currently used for signing the app, which needs to be renewed every year. This can be done via the `Package.appxmanifest` project explorer unter the section `Packaging`. 