# Ferramentas:

- Aseprite - Criar/editar Spritesheet onde é possível salvar como PNG, formato ideal para ser usado no MonoGame.  
- Tiled - Usa os sprites criados no Aseprite para criar Tile Map por camadas e salvar as coordenadas como JSON (sempre usar a opção de incorporar conjunto de tiles no arquivo JSON para não ser necessário buscar informações externas nos arquivos TSX).  
- DotTiled - Dependência instalada via NuGet no Visual Studio, este lê o JSON criado no Tiled para renderizar o mapa no MonoGame.  

# Uso:

- Os arquivos com as imagens/texturas são inseridos no MonoGame, arquivo Content.mgcb. Ex.: TIL01.png e OBJ01.png. Estes arquivos são carregados no método Game1.LoadContent().  
- Dentro de LoadContent passamos as texturas para a classe TilemapRenderer, esta classe é responsável por ler as coordenadas do JSON e renderizar as texturas dos arquivos, lá é usada a dependência do DotTiled.  
- DotTiled serve apenas para ler os tiles/texturas do JSON em uma lista, quem renderiza é o MonoGame com spriteBatch.Draw().  
- O DotTiled tem classes internas predefinidas como TileLayer e ObjectLayer, essas classes estão no JSON e se referem as camadas de texturas criadas no Tiled, no exemplo a TileLayer recebe as coordenada para o arquivo TIL01 e a ObjectLayer recebe sobre o arquivo de textura OBJ01.  

# Definições do jogo:

- São apenas 4 direções disponíveis para movimentação, assim como no Bomberman.  
- As personagens criam magias elementais que são colocadas no chão e detonam após alguns segundos, mesmo estilo de Bomberman.  
- As magias se espalham em esferas de chamas ou gelo para as 4 direções disponíveis.  
- As magias tem alcance limitado que podem ser amplicados com Powerups espalhados pelo mapa.  
- As magias elementais devem ter efeitos distintos, como por exemplo, magia de fogo derrota monstro de gelo com 1 golpe.  

![Conceito atual](Content/Tiles/MAP01.png)

# Próximos passos:

- 1 - Criar ajuste automatico escalando o tamanho da tela de jogo com a resolução atual da maquina que está executando o jogo.  
- 2 - Criar classes de mapeamento de Joystick/Keyboard preparadas para receber alterações via menu em tempo de execução.  
- 3 - Criar classe que transfere comandos mapeados para a personagem e movimentar ela na tela.  
- 4 - Criar classes de colisão com objetos para impedir que a personagem atravesse paredes ou saia da tela.  
- 5 - Criar interação da personagem com objetos coletaveis ou destrutiveis.  
- 6 - Criar classe ou metodo para primeiro spawn de inimigos no mapa apartir da posição inicial definida no JSON do Tiled.  
- 7 - Criar interação da personagem com inimigos (monstro recebe ataque).  
- 8 - Criar interação dos inimigos com as personagens (personagem recebe ataque), os inimigos precisam detectar a presença do jogador e usar seus golpes disponiveis.  
