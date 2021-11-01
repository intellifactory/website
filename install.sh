#!/bin/bash

# Initialize git submodules
git submodule update --init --recursive

echo "Copy legal/site-docs/if.com/ under src/Online/legal/"
cp -rT legal/site-docs/intellifactory.com/ src/Online/legal/

echo "Copy blogposts under /src/Online/posts"
cp -rT blogs/user src/Online/posts

echo "Copy blog assets under /src/Online/assets"
cp -rT blogs/assets src/Online/wwwroot/assets

echo "Copy post.html to /src/Online/post.html"
cp -rT website-template/public/post.html src/Online/post.html

echo "Copy contact.html to /src/Online/contact.html"
cp -rT website-template/public/contact.html src/Online/contact.html

echo "Copy css/tailwind/tailwind.min.css to /src/Online/wwwroot/css/tailwind/tailwind.min.css"
cp -rT website-template/public/css/tailwind/tailwind.min.css src/Online/wwwroot/css/tailwind/tailwind.min.css

echo "Copy css/tailwind/tailwind.css to /src/Online/wwwroot/css/tailwind/tailwind.css"
cp -rT website-template/public/css/tailwind/tailwind.css src/Online/wwwroot/css/tailwind/tailwind.css
