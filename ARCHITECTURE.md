# Arquitetura

## Fluxo de dados

1. O aplicativo WPF inicia `codex.exe app-server` sem abrir console.
2. O cliente C# envia `initialize` e `initialized` por JSON-RPC em `stdin/stdout`.
3. A infraestrutura consulta os limites e a atividade da sessao autenticada.
4. O Core normaliza percentuais, horarios e os sete dias de atividade.
5. O ViewModel atualiza o card a cada minuto.

## Controle local

O processo principal hospeda controllers ASP.NET Core exclusivamente em `127.0.0.1:5087`:

- `GET /api/usage`
- `GET /api/app/status`
- `POST /api/app/open`
- `POST /api/app/minimize`
- `POST /api/app/off`

O CLI usa esses endpoints. Como o bind e somente de loopback, nenhuma maquina da rede consegue acessar o backend.

## Inicializacao do Windows

O instalador oferece a tarefa opcional de criar a entrada `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\CodexMeter`.
O menu de contexto do widget altera a mesma configuracao.

