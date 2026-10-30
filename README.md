# Codex Meter Desktop

Widget compacto para Windows que mostra os limites de uso da conta Codex autenticada no computador.

> Projeto independente, sem afiliacao com a OpenAI.

## O que ele faz

- Abre como um card de `236 x 148 px` no canto inferior direito.
- Fica sempre acima das outras janelas.
- Pode ser arrastado e salva a ultima posicao.
- Mostra a janela de 5 horas, o limite semanal e o proximo horario de renovacao.
- Atualiza automaticamente a cada minuto.
- Pode iniciar junto com o Windows.
- Pode ser controlado pelo CMD.

## Requisitos para desenvolvimento

- Windows 10 ou 11.
- .NET SDK 10.
- Codex Desktop ou Codex CLI instalado e autenticado.

O instalador publicado e autocontido e nao exige a instalacao manual do .NET.

## Executar o projeto

```powershell
dotnet run --project src/CodexMeter.Desktop
```

O aplicativo inicia `codex app-server` de forma oculta e consulta:

- `account/rateLimits/read`
- `account/usage/read`

Nenhuma senha, token OAuth ou chave da conta e copiada para o aplicativo.

No Windows, o executavel e descoberto automaticamente tanto pelo `PATH` quanto pela pasta interna
versionada do Codex Desktop em `%LOCALAPPDATA%\OpenAI\Codex\bin`.

## Comandos no CMD

O instalador adiciona `codex-meter` ao `PATH` do usuario:

```bat
codex-meter open
codex-meter config
codex-meter minimize
codex-meter minimizer
codex-meter off
codex-meter status
```

`code-meter` também funciona como apelido, por exemplo `code-meter config`. Na janela de
personalização, ajuste as dimensões e marque os cantos que devem continuar arredondados.
Em **Visual do card**, escolha entre oito modelos, incluindo Chama claro/escuro e Barra.
Em **Informações**, escolha se o limite semanal e a renovação aparecem; a renovação pode
ser exibida como horário ou tempo restante.
As alterações aparecem no card durante a edição; use **Salvar visual** para guardá-las ou
**Cancelar** para restaurar o visual anterior.
Na seção **Borda**, ajuste a espessura de 0 a 8 px, escolha uma cor da lista ou informe um
código hexadecimal como `#FF8844`. A opção **Cor do tema** usa a borda preta original.
Em **Tamanho do card**, a largura pode variar de 220 a 400 px e a altura de 140 a 240 px.

O card apresenta o percentual restante da janela de cinco horas, o limite semanal e o horário
de renovação quando disponíveis.

O nome `codex-meter` evita substituir o executavel oficial `codex`.

## Instalacao

1. Baixe `CodexMeter-Setup.exe` na pagina de Releases.
2. Execute o instalador.
3. Marque **Iniciar o Codex Meter com o Windows** se desejar.
4. Conclua a instalacao e abra o aplicativo.

A opcao tambem pode ser alterada depois clicando com o botao direito no widget.

## Compilar

```powershell
.\scripts\build-installer.ps1
```

O script publica os dois executaveis autocontidos em `artifacts/` e usa o Inno Setup 6 para gerar:

```text
artifacts/installer/CodexMeter-Setup.exe
```

## Arquitetura

```text
CodexMeter.Desktop
  Controllers locais em 127.0.0.1:5087
  Widget WPF sem moldura
          |
CodexMeter.Infrastructure
  Cliente JSON-RPC do codex app-server
          |
CodexMeter.Core
  Modelos e normalizacao dos limites
```

- `CodexMeter.Core`: modelos, contratos e normalizacao.
- `CodexMeter.Infrastructure`: comunicacao com `codex app-server`.
- `CodexMeter.Desktop`: interface WPF e controllers ASP.NET Core locais.
- `CodexMeter.Cli`: comandos do terminal.
- `CodexMeter.SmokeTests`: testes sem dependencias externas.

## Seguranca

- A API escuta apenas em `127.0.0.1`.
- Nao ha porta exposta para a rede local.
- A sessao autenticada continua sob controle do Codex CLI.
- O aplicativo mostra apenas informacoes de consumo e limites.
- Por esse motivo nao ha senha adicional no widget.

## Testes

```powershell
dotnet build CodexMeter.slnx
dotnet run --project tests/CodexMeter.SmokeTests
```

## Licenca

MIT. Consulte [LICENSE](LICENSE).

O simbolo OpenAI exibido no widget pertence a OpenAI e e usado apenas para indicar a integracao
direta com o Codex. O projeto permanece independente e sem afiliacao oficial.

