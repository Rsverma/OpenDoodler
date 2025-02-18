// See https://aka.ms/new-console-template for more information
//Add-Migration InitialCreate
//Update-Database
using Microsoft.EntityFrameworkCore;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;

DataContext context = new DataContext();
GraphicRepository gr = new(context);
var tableNames = context.Model.GetEntityTypes()
    .Select(t => t.GetTableName())
    .Distinct()
    .ToList();
context.Database.ExecuteSqlRaw("Delete from [Graphics]");
context.Database.ExecuteSqlRaw("DELETE FROM SQLITE_SEQUENCE WHERE name='Graphics';");
List<GraphicEntity> graphicEntities = gr.GetAllGraphics();
GraphicEntity entity1 = new GraphicEntity
{
    Name = "peep-43",
    SVGText = File.ReadAllText("resources\\peep-43.svg")
};
GraphicEntity entity2 = new GraphicEntity
{
    Name = "peep-61",
    SVGText = File.ReadAllText("resources\\peep-61.svg")
};
GraphicEntity entity3 = new GraphicEntity
{
    Name = "undraw_love_is_in_the_air_4uud",
    SVGText = File.ReadAllText("resources\\undraw_love_is_in_the_air_4uud.svg")
};
gr.AddNewGraphic(entity1);
gr.AddNewGraphic(entity2);
gr.AddNewGraphic(entity3);
