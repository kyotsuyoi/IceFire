Ferramentas:

Aseprite - Criar/editar Spritesheet onde é possível salvar como PNG, formato ideal para ser usado no MonoGame.
Tiled - Usa os sprites criados no Aseprite para criar Tile Map por camadas e salvar as coordenadas como JSON (sempre usar a opção de incorporar conjunto de tiles no arquivo JSON para não ser necessário buscar informações externas nos arquivos TSX).
DotTiled - Dependência instalada via NuGet no Visual Studio, este lê o JSON criado no Tiled para renderizar o mapa no MonoGame.

Uso:
1 - Os arquivos com as imagens/texturas são inseridos no MonoGame, arquivo Content.mgcb. Ex.: TIL01.png e OBJ01.png. Estes arquivos são carregados no método Game1.LoadContent().
2 - Dentro de LoadContent passamos as texturas para a classe TilemapRenderer, esta classe é responsável por ler as coordenadas do JSON e renderizar as texturas dos arquivos, lá é usada a dependência do DotTiled.
3 - DotTiled serve apenas para ler os tiles/texturas do JSON em uma lista, quem renderiza é o MonoGame com spriteBatch.Draw().
4 - O DotTiled tem classes internas predefinidas como TileLayer e ObjectLayer, essas classes estão no JSON e se referem as camadas de texturas criadas no Tiled, no exemplo a TileLayer recebe as coordenada para o arquivo TIL01 e a ObjectLayer recebe sobre o arquivo de textura OBJ01.