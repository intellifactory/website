namespace Templates

open WebSharper
open WebSharper.UI.Templating

type PostTemplate = Template<"../Online/post.html", serverLoad=ServerLoad.WhenChanged>
type ContactTemplate = Template<"../Online/contact.html", serverLoad=ServerLoad.WhenChanged>
type BlogsTemplate = Template<"../Online/blogs.html", serverLoad=ServerLoad.WhenChanged>
type AuthorTemplate = Template<"../Online/author.html", serverLoad=ServerLoad.WhenChanged>
type CategoryTemplate = Template<"../Online/category.html", serverLoad=ServerLoad.WhenChanged>
type OSSTemplate = Template<"../Online/oss.html", serverLoad=ServerLoad.WhenChanged>
type Error404Template = Template<"../Online/404.html", serverLoad=ServerLoad.WhenChanged>
type JobsTemplate = Template<"../Online/jobs.html", serverLoad=ServerLoad.WhenChanged>
