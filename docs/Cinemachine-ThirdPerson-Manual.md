# Manual: Câmera Third-Person com Cinemachine

Guia de referência rápida para configurar uma câmera third-person "over the shoulder"
com Cinemachine 3.x, com colisão em interiores e sem causar motion sickness. Feito pra
consultar em qualquer projeto novo — não é um log do que foi feito neste projeto.

---

## 1. Componentes essenciais (o que usar e pra quê)

| Componente | Pra quê serve | Quando usar |
|---|---|---|
| `CinemachineThirdPersonFollow` | Braço sobre o ombro: posiciona a câmera em relação a um alvo, com offset de ombro. | Base de qualquer câmera TPS over-the-shoulder. |
| `CinemachineDeoccluder` | Preserva a linha de visão ao alvo, empurrando a câmera pra não atravessar paredes/objetos. | Sempre que houver geometria entre câmera e personagem (interiores, corredores). |
| `CinemachineDecollider` | Resolve colisão simples (tira a câmera de dentro de geometria) e resolve terreno, mas **não** preserva linha de visão. | Camada extra de segurança, ou quando não precisa manter o personagem sempre visível. |
| `CinemachineConfiner3D` | Confina a posição da câmera dentro de um volume 3D. | Salas/corredores muito apertados, como cinto de segurança adicional ao Deoccluder. |
| `CinemachineImpulseSource` + `CinemachineImpulseListener` | Shake de câmera (dano, explosão) desacoplado da lógica de follow/aim. | Sempre que precisar de screen shake — nunca mexer direto na posição da câmera de gameplay. |
| `CinemachineBrain.UpdateMethod` | Define em qual clock a câmera atualiza (Update/FixedUpdate/SmartUpdate). | **Smart Update** se o personagem usa `CharacterController` (que se move em `Update`, não `FixedUpdate`) — evita jitter. |

## 2. Setup mínimo recomendado (checklist)

1. `CinemachineCamera` com `Follow` e `LookAt` apontando pra um `CameraTarget` vazio
   (filho do personagem, altura dos ombros) — **não** aponte direto pro root do
   personagem, senão não dá pra rotacionar a câmera independente do corpo.
2. Adicionar `CinemachineThirdPersonFollow` (Body). Valores de partida sãos:
   `Damping ~0.1/0.1/0.1`, `ShoulderOffset ~0.8/0/0`, `CameraDistance ~3-4`.
3. Adicionar `CinemachineDeoccluder`:
   - `Collide Against`: só os layers de geometria sólida (paredes, chão, mobília).
   - `Ignore Tag`: a tag do próprio personagem (senão a câmera tenta esquivar do
     próprio corpo do jogador).
   - `Minimum Occlusion Time ~0.05s`: ignora oclusões de 1 frame (grade fina, poste
     passando rápido no campo de visão) — evita a câmera reagindo a toda hora.
   - `Smoothing Time ~0.15-0.2s`: evita "bombeamento" da câmera em ambientes cheios de
     props próximos uns dos outros.
   - `Damping When Occluded` é o botão mais importante pra sensação de "solavanco": ele
     controla a velocidade de reação *ao entrar* em colisão. O padrão do componente é
     baixo (reativo/brusco) — se a câmera parecer bater seco na parede, suba esse valor
     (algo como `0.5-0.7`) antes de mexer em qualquer outra coisa.
4. Rotação da câmera (yaw/pitch) por **código**, não pelo `CinemachineInputAxisController`
   — dá controle direto sobre sensibilidade, clamp vertical e limites de segurança (ver
   seção 3). Setar `Rotation Control = None` no `CinemachineCamera` quando a rotação vier
   de fora.
5. `CinemachineBrain.UpdateMethod = Smart Update` se o personagem usa `CharacterController`.

## 3. Motion sickness — o essencial

Poucas regras cobrem a maior parte do problema:

1. **FOV em terceira pessoa: 75°–85°** é a faixa confortável (diferente de primeira
   pessoa, que costuma precisar de 90–110° por não ter o corpo como referência visual).
   Sempre exponha um slider de FOV nas opções — o ideal varia com tamanho de tela e
   distância do jogador.
2. **Nunca mude FOV bruscamente.** Qualquer zoom/kick de FOV precisa ser suave (lerp
   lento, ~0.3-0.5s pra completar) — mudança brusca de FOV é uma das causas mais citadas
   de náusea.
3. **Trave a velocidade de rotação por frame.** Picos de input (spike de `deltaTime`,
   cursor warp ao trocar de janela, frame drop) podem girar a câmera de forma absurda
   num único frame — sempre clampar o delta de yaw/pitch aplicado por frame como rede de
   segurança, independente da sensibilidade configurada.
4. **Shake de câmera sempre via Impulse**, nunca direto na posição/rotação da câmera de
   gameplay — assim dá pra oferecer um toggle "reduzir tremor" sem afetar o resto do
   sistema.
5. **Evite Dutch (roll) automático** — inclinar a câmera lateralmente sem ação explícita
   do jogador é uma das maiores causas de desconforto reportadas.
6. **Ofereça as opções, não decida por todo mundo**: FOV slider, toggle de camera
   shake, toggle de motion blur. Sensibilidade e conforto variam muito por pessoa.

## 4. Agachar em espaços apertados (tetos baixos)

Esse é um caso que quebra a câmera de um jeito difícil de debugar se você não souber a
causa: o `CameraTarget` normalmente fica numa altura fixa (altura dos ombros, calibrada
pro personagem em pé). Se o jogador agacha embaixo de uma plataforma baixa, esse ponto
fixo fica **dentro da geometria do teto** — e aí o Deoccluder não tem mais uma direção
limpa pra resolver (ele tenta manter linha de visão a um alvo que já está atravessado por
uma parede), o que produz uma câmera visivelmente instável/errática, não só "meio
grudenta".

A causa não é o Deoccluder estar mal configurado — é o alvo dele estar num lugar
impossível. A solução certa é nunca deixar isso acontecer:

1. Exponha o estado de agachado do seu controller (um evento simples tipo
   `event Action<bool> CrouchStateChanged` já resolve).
2. No script que controla a rotação da câmera, escute esse evento e interpole
   (`Lerp`, não um snap instantâneo) a altura local do `CameraTarget` entre um valor "em
   pé" e um "agachado" — geralmente uns 0.5-0.6 unidades mais baixo já resolve a maioria
   dos casos.

Se isso sozinho não for suficiente em corredores muito apertados (o braço da câmera
ainda não cabe mesmo mais baixo), o próximo ajuste é encolher também o `CameraDistance`
do `ThirdPersonFollow` enquanto agachado — um braço mais curto precisa de menos espaço
livre pra se resolver. É basicamente o mesmo padrão de evento, só que interpolando outro
valor.

## 5. Pegadinhas de controller que parecem bug de câmera (mas não são)

Duas causas raiz comuns que *parecem* bug de física/input, mas na real quebram o jogo
inteiro, câmera incluída:

- **`Animator.applyRootMotion = true` + movimentação manual via `CharacterController`.**
  Se o seu controller já calcula velocidade e chama `CharacterController.Move()` no
  código (a abordagem mais comum em controllers simples), deixar `Apply Root Motion`
  ligado no Animator faz a Unity aplicar a root motion *por cima*, também via
  `CharacterController.Move()`, automaticamente. Dois `Move()` brigando no mesmo frame.
  Sintoma clássico: ações específicas (pular, por exemplo) funcionam em alguns estados de
  animação e falham em outros, sem motivo óbvio. Se o controller é 100% manual, `Apply
  Root Motion` deve ficar desligado.
- **Ground check com margem apertada demais.** Um erro comum é montar a esfera/raycast de
  "estou no chão" com uma margem tão pequena entre o fundo da cápsula e o alcance do
  check que qualquer variação mínima de posição (movimento rápido, irregularidade de
  colisão) derruba o resultado pra `false` bem na hora que o jogador aperta pular. Se
  pular funciona parado mas falha correndo, comece por aqui: aumente o raio/alcance do
  ground check pra ter uma folga real (não alguns milímetros).

## 6. Fora de escopo pra um template simples

Coisas como **troca dinâmica de ombro / corner peek**, câmeras de mira dedicadas,
vinheta dinâmica em giros rápidos, e curvas de aceleração de sensibilidade são técnicas
válidas de jogos AAA, mas são ajuste fino de *gameplay feel* — só vale a pena investir
nelas quando o projeto já tem um design de combate/level definido que precise disso.
Num template base, adicionar isso é complexidade sem benefício claro.

---

## 7. Referências

### Documentação oficial Unity Cinemachine
- [Cinemachine Deoccluder — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDeoccluder.html)
- [Cinemachine Decollider — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDecollider.html)
- [Cinemachine Confiner 3D — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineConfiner3D.html)
- [Camera control and transitions — Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/concept-camera-control-transitions.html)
- [Class CinemachineBrain.UpdateMethod — API](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/api/Unity.Cinemachine.CinemachineBrain.UpdateMethod.html)

### Jitter / occlusion tuning (discussões técnicas)
- [Cinemachine: avoid camera clipping while allowing obstructions — Unity Discussions](https://discussions.unity.com/t/cinemachine-avoid-camera-clipping-while-allowing-obstructions/1573849)
- [What Cinemachine UpdateMethod can I use? — Unity Discussions](https://discussions.unity.com/t/what-cinemachiene-updatemethod-can-i-use/1628761)
- [Fix: Unity Cinemachine Camera Jitter and Stutter — Bugnet Blog](https://bugnet.io/blog/fix-unity-cinemachine-camera-jitter)

### Motion sickness / conforto / acessibilidade
- [The Last of Us Part II: Accessibility Features Detailed — Naughty Dog](https://www.naughtydog.com/blog/the_last_of_us_part_ii_accessibility_features_detailed)
- [If the game uses field of view, set an appropriate default — Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/if-the-game-uses-field-of-view-3d-engine-only-set-an-appropriate-default-for-the-expected-viewing-environment/)
- [How to Fix a Camera Field of View Causing Motion Sickness — Bugnet Blog](https://bugnet.io/blog/how-to-fix-camera-fov-causing-motion-sickness)
- [Alleviating motion sickness in first-person video games — Nicolas Busseneau](https://nicolas.busseneau.fr/en/blog/2020/09/alleviating-motion-sickness-in-first-person-video-games)

### Shake / juice em Cinemachine
- [Adding Camera Shake in Unity Using Cinemachine — Medium](https://medium.com/@wesley.n.campbell/adding-camera-shake-in-unity-using-cinemachine-80450b075f52)
- [Unity Camera Control Best Practices: Performance, Flexibility, and Feel](https://exoa.dev/blog/unity-camera-control-best-practices)

### Fundamentos de matemática de câmera (opcional, aprofundado)
- [Math for Game Programmers: Juicing Your Cameras With Math — Squirrel Eiserloh, GDC 2016 (YouTube)](https://www.youtube.com/watch?v=tu-Qe66AvtY)
