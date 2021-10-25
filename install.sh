#!/bin/bash

# Initialize git submodules
git submodule update --init --recursive

echo "Copy legal/site-docs/if.com/ under src/Online/legal/"
cp -rT legal/site-docs/intellifactory.com/ src/Online/legal/

echo "Copy blogposts under /src/Online/posts"
cp -rT blogs/user src/Online/posts

echo "Copy blog assets under /src/Online/assets"
cp -rT blogs/assets src/Online/wwwroot/assets
