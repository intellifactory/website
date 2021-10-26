# Initialize git submodules
git submodule update --init --recursive

Write-Output "Copy legal files"

xcopy .\legal\site-docs\intellifactory.com\* .\src\Online\legal\ /s /e

Write-Output "Copy blog posts files"

xcopy .\blogs\user\* .\src\Online\posts\ /s /e

Write-Output "Copy blog asset files"

xcopy .\blogs\assets\* .\src\Online\wwwroot\assets\ /s /e

Write-Output "Copy blog.html to /src/Online/post.html"
xcopy website-template/public/blog.html src/Online/post.html

Write-Output "Copy css/tailwind/tailwind.min.css to /src/Online/wwwroot/css/tailwind/tailwind.min.css"
xcopy website-template/public/css/tailwind/tailwind.min.css src/Online/wwwroot/css/tailwind/tailwind.min.css
