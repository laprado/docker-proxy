# docker compose nginx proxy

Nesse repositório temos um de exemplo de como configurar o nginx como um proxy usando o docker. Estamos utilizando o docker porém, o mesmo exemplo pode ser executado numa VM dedicada ao nginx. Não foi testado numa máquina Windows, mas deve funcionar também. Basta usar o mesmo arquivo .conf passado no volume mapeado no arquivo docker-compose com as devidas adaptações de caminhos para funcionar no windows.

### pré-requisitos

* instalar o docker engine + docker compose no linux.
* ou, testar a imagem diretamente no windows, também existe versão do nginx para windows, como o nginx trata-se um http server ele concorre com o IIS, portanto é necessário configurá-lo para escutar portas diferentes do IIS ou não ter o IIS rodando.
* As portas de saída 443 ou 80 devem ser liberadas na máquina host (quando em produção) para que as mesmas possam ser escutadas pelo nginx.

## primeiro passo executar somente como webserver:

Garanta que o arquivo config/nginx/conf.d/default.conf esteja com segundo e terceiro blocos comentados e que o primeiro bloco esteja descomentado para esse teste.

```Terminal
docker compose -p docker-proxy up -d
```

stop:
```Terminal
docker compose -p docker-proxy down
```

Você pode abrir o docker desktop e visualizar um container em execução chamado docker-proxy, ele possui um único serviço: proxy-service, se você licar na porta 80 (ou acessar http://localhost) verá uma página padrão do nginx.

## segundo passo executar somente como proxy:

Coloque uma api de teste para ser executada e receber qualquer rota, essa api será o alvo do proxy feito pelo nginx. O projeto da pasta api foi feito para isso. Essa api fará o papel de um serviço externo, e por isso não está no arquivo do docker-compose. Para executá-lo é necessário ter Dotnet SDK 7.0 instalado. Abra um terminal e a partir da pasta api execute o seguinte comando:

```Terminal
dotnet run
```

Com isso temos uma api para testes e investigações sendo executada. Tente executar no browser mesmo a seguinte url: http://localhost:5100/api/rota/teste
Essa api deve aceitar qualquer rota, menos vazia "http://localhost:5100". Deve retornar dados do header importantes para investigação do comportamento do nosso proxy, como a própria rota e o RemoteIpAddress.

### ajustar o arquivo de configuração do nginx

No arquivo config/nginx/conf.d/default.conf comente o bloco do passo 1, e descomente o bloco do passo 2. 

Obs: no passo 2 temos a configuração: "proxy_pass http://192.168.15.20:5100;", aqui é necessário colocar o ip da máquina onde está rodando a api, para que o nginx (dentro do docker) possa localizar esse host. Quando for para colocar em produção esse será o caminho real do serviço de apis alvo do proxy.

Reinicie o serviço com docker:

```Terminal
docker compose -p docker-proxy down
docker compose -p docker-proxy up -d
```

Ou reinicie o serviço se estiver instalado local (sudo no linux, ou como administrador no windows):
```Terminal
nginx -s reload
```

### executar alguns testes com postman ou outro app:

Na pasta postman desse projeto temos uma collection de exemplo para testar o conteúdo proposto até aqui. Podemos ver que tanto o body quando a rota e outras variáveis do header são passadas para o serviço alvo do proxy. Precisamos validar o RemoteIpAddress que será exibido no response da api, de forma a garantir que sempre estejamos recebendo o ip de onde se encontra o servidor proxy. Ou seja, seria interessante executar os testes das apis contra o proxy e contra o host diretamente, deveremos ter ips diferentes nesses casos, desde que executados a partir de máquinas diferentes do nosso host.

```Powershell
iwr -Uri "http://192.168.15.20/teste/rapido" -UseBasicParsing | Write-Host
```

## terceiro passo preparar para produção

Necessário que tenhamos uma forma de executar testes no ambiente de produção para validar que as configurações aqui apresentadas sejam o suficiente.

Vamos ajustar o arquivo config/nginx/conf.d/default.conf para que o mesmo possa ser executado em produção.

Primeiro precisamos alterar o server_name para o dns correto do servidor de produção. Mudaremos de localhost para api.servico.empresa.com.br. Deve ser alterado nas duas entradas do tipo server { server_name api.servico.empresa.com.br}.

Comentar o bloco do passo 1 e descomentar os blocos dos passos 2 e 3. No passo 2 é necessário alterar a URL do endereço da entrada proxy_pass para apontar para a api real alvo do proxy. Não estamos utilizando a opção rewrite do nginx, pois trata-se de um redirect e isso anularia o papel de proxy que queremos de nosso exemplo.

Existem duas entradas do tipo server {}, a primeira configura o servidor de proxy, e a segunda seria responsável por efetuar o redirect da porta 80 para 443. Para que essa configuração tenha efeito é necessário remover a entrada lista 80 da primeira entrada server {}.

Com relação ao bloco 3 o mesmo é responsável por ouvir a porta 443 e executar serviços sob o protocolo TLSv1.2 e TLSv1.3, precisamos informar os dados de certificado para o nosso host. No arquivo docker-compose para o serviço proxy-service temos a montagem de um volume que aponta para config/certificado da máquina host onde devem estar localizados os arquivos dos certificados a serem configurados no nginx.

A última entrada ssl_dhparam pode ficar comentada, a mesma está presente caso seja necessário implementar um nível a mais de segurança baseado em "Diffie-Hellman key exchange", nesse link temos um exemplo de como configurar essa camada https://scaron.info/blog/improve-your-nginx-ssl-configuration.html. De qualquer forma também podemos configurar nossos serivços de nuvem para que o servidor proxy aceite conexões apenas de ips permitidos.

Ainda no bloco 3 do arquivo podemos comentar ou simplesmente remover os headers "X-*" caso não sejam necessários para a api alvo do proxy.

Caso seja efetuado o deploy da solução usando o docker-compose temos algumas opções:
1. ajustar a entrada do arquivo docker-compose.yml, mudando as variáveis do environment: NGINX_HOST, NGINX_PORT para o dns de produção e porta 443. Executar no servidor de produção:

```Terminal
docker compose -p docker-proxy down
docker compose -p docker-proxy up -d
```

Obs: pode ser utilizada opção para que o container seja reiniciado sempre que o docker for iniciado.

2. Se a máquina está com nginx instalado direto: copie o arquivo default.conf para o local /etc/nginx/conf.d/default.conf, ou para o diretório respectivo do windows. Garanta que o nginx esteja instalado e configurado para sempre subir assim que a máquina iniciar, esse é o comportamento padrão quando o mesmo é instalado. Se o nginx estiver rodando execute o seguinte comando após a cópia do arquivo:

```Terminal
nginx -s reload
```

Obs: por padrão, quando o nginx é instalado o mesmo se configura para sempre iniciar junto com o sistema operacional, porém é bom confirmar se o serviço está de pé após um restart da máquina.