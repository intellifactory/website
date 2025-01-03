# Initialize git submodules
git submodule update --init --recursive

Write-Output "Copy legal files"

xcopy .\legal\site-docs\intellifactory.com\* .\src\Online\legal\ /s /e

Write-Output "Copy blog posts files"

xcopy .\blogs\user\* .\src\Online\posts\ /s /e

Write-Output "Copy blog asset files"

xcopy .\blogs\assets\* .\src\Online\wwwroot\assets\ /s /e

Write-Output "Copy post.html to \src\Online\post.html"
xcopy website-template\public\post.html src\Online\ /Y

Write-Output "Copy contact.html to \src\Online\contact.html"
xcopy website-template\public\contact.html src\Online\ /Y

Write-Output "Copy blogs.html to \src\Online\blogs.html"
xcopy website-template\public\blogs.html src\Online\ /Y

Write-Output "Copy author.html to \src\Online\author.html"
xcopy website-template\public\author.html src\Online\ /Y

Write-Output "Copy category.html to \src\Online\category.html"
xcopy website-template\public\category.html src\Online\ /Y

Write-Output "Copy oss.html to \src\Online\oss.html"
xcopy website-template\public\oss.html src\Online\ /Y

Write-Output "Copy 404.html to \src\Online\404.html"
xcopy website-template\public\404.html src\Online\ /Y

Write-Output "Copy jobs.html to \src\Online\jobs.html"
xcopy website-template\public\jobs.html src\Online\ /Y

Write-Output "Copy css\tailwind\tailwind.min.css to src\Online\wwwroot\css\tailwind\tailwind.min.css"
xcopy website-template\public\css\tailwind\tailwind.min.css src\Online\wwwroot\css\tailwind\ /Y

Write-Output "Copy css\tailwind\tailwind.css to src\Online\wwwroot\css\tailwind\tailwind.css"
xcopy website-template\public\css\tailwind\tailwind.css src\Online\wwwroot\css\tailwind\ /Y

Write-Output "Copy templates image assets to src\Online\wwwroot\images"
xcopy website-template\src\assets\images\* src\Online\wwwroot\images\ /s /e /Y

Write-Output "Copy templates css assets to src\Online\wwwroot\css"
xcopy website-template\src\assets\css\* src\Online\wwwroot\css\ /s /e /Y
