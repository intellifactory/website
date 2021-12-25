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

echo "Copy blogs.html to /src/Online/blogs.html"
cp -rT website-template/public/blogs.html src/Online/blogs.html

echo "Copy author.html to /src/Online/author.html"
cp -rT website-template/public/author.html src/Online/author.html

echo "Copy category.html to /src/Online/category.html"
cp -rT website-template/public/category.html src/Online/category.html

echo "Copy oss.html to /src/Online/oss.html"
cp -rT website-template/public/oss.html src/Online/oss.html

echo "Copy 404.html to /src/Online/404.html"
cp -rT website-template/public/404.html src/Online/404.html

echo "Copy jobs.html to /src/Online/jobs.html"
cp -rT website-template/public/jobs.html src/Online/jobs.html

echo "Copy css/tailwind/tailwind.min.css to /src/Online/wwwroot/css/tailwind/tailwind.min.css"
cp -rT website-template/public/css/tailwind/tailwind.min.css src/Online/wwwroot/css/tailwind/tailwind.min.css

echo "Copy css/tailwind/tailwind.css to /src/Online/wwwroot/css/tailwind/tailwind.css"
cp -rT website-template/public/css/tailwind/tailwind.css src/Online/wwwroot/css/tailwind/tailwind.css

echo "Copy templates image assets to /src/Online/wwwroot/images"
cp -rT  website-template/src/assets/images src/Online/wwwroot/images

echo "Copy templates css assets to /src/Online/wwwroot/css"
cp -rT  website-template/src/assets/css src/Online/wwwroot/css
