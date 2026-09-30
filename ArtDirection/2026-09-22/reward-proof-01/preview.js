'use strict';
// Presentation-only study over a recorded width-9 Unity camera. No game state is connected.
const canvas = document.getElementById('arena');
const ctx = canvas.getContext('2d');
const image = new Image();
const scene = new Image();
const size = document.getElementById('size');
const density = document.getElementById('density');
const radius = document.getElementById('radius');
const gray = document.getElementById('gray');
const status = document.getElementById('status');
const collect = document.getElementById('collect');
const sweep = document.getElementById('sweep');
const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
const hero = {x:540, y:1200};
const pixelsPerUnit = 120;
// Full unchanged source, fixed contact root; alpha>16 width is 835 source pixels.
const sourceRoot = {x:636, y:1122};
const strongWidth = 835;
const layout = [[0.72,-0.42],[-1.65,-0.55],[1.5,-1.8],[-0.6,-2.7],[0.1,2.2],
  [-2.7,0.2],[2.4,1.4],[-1.5,2.6],[2.75,-2.8],[-2.6,-2.5],[1.3,3.4],[-0.5,4.4],
  [0.8,-3.9],[-1.7,-4.1],[3.4,-0.6],[-3.4,1.4],[2.3,4.0],[-2.45,4.1],
  [-3.45,-1.2],[0.4,-1.6],[1.8,-0.7],[-1.0,1.1],[2.6,2.6],[-2.7,2.8],
  [0.1,-5.0],[2.1,-4.3],[-1.0,-5.3],[0.7,5.3],[-1.25,5.2],[3.35,0.4],
  [-3.4,-3.5],[3.4,-4.2],[-0.4,3.2],[1.8,1.9],[-2.0,0.5],[0.7,0.7]];
let drops = [];
let pulseAt = -1000;
let raf = 0;
let ready = false;
function setStatus(message) { status.textContent = message; }
function reset() {
  drops = layout.slice(0, Number(density.value)).map(([x,y]) => ({x:hero.x+x*pixelsPerUnit,y:hero.y+y*pixelsPerUnit/2, state:'ground', started:0, duration:0}));
  pulseAt = -1000;
  setStatus(drops.length + ' örnek ödül · değerler ve konumlar oyun verisine bağlı değil.');
  updateButtons();
  requestDraw();
}
function nearby(drop) { return Math.hypot((drop.x-hero.x)/pixelsPerUnit, (drop.y-hero.y)*2/pixelsPerUnit)<=1; }
function updateButtons() {
  collect.disabled = !ready || !drops.some(d => d.state === 'ground' && nearby(d));
  sweep.disabled = !ready || !drops.some(d => d.state === 'ground');
}
function begin(all) {
  const now = performance.now();
  const candidates = drops.filter(d => d.state === 'ground' && (all || nearby(d)));
  for (const drop of candidates) {
    drop.state = reduceMotion.matches ? 'gone' : 'flying';
    drop.started = now;
    drop.duration = all ? 420 : 220;
  }
  if (candidates.length) pulseAt = now;
  setStatus(all ? 'Kat sonu: kalan ödüller birlikte kahramana geliyor. XP ve altın uçuş başlamadan kazanılmış olur.' : '1 birim içindeki örnek toplandı. Görsel uçuş, ödülü geciktirmiyor.');
  updateButtons();
  requestDraw();
}
function drawDrop(drop, now) {
  if (drop.state === 'gone') return false;
  let x=drop.x, y=drop.y, alpha=1, shrink=1;
  if (drop.state === 'flying') {
    const t=Math.min(1,(now-drop.started)/drop.duration);
    if(t>=1){drop.state='gone';return false;}
    const ease=t*t*(3-2*t);
    x+=(hero.x-x)*ease;
    y+=(hero.y-30-y)*ease - Math.sin(t*Math.PI)*22;
    shrink=1-0.5*t;
    alpha=1-Math.max(0,(t-0.65)/0.35);
  }
  const scale=Number(size.value)*pixelsPerUnit/strongWidth*shrink;
  ctx.save();
  ctx.globalAlpha=alpha;
  if(drop.state==='ground') {
    ctx.fillStyle='rgba(5,8,19,.38)';ctx.beginPath();ctx.ellipse(x,y-1,Number(size.value)*pixelsPerUnit*.45,Number(size.value)*pixelsPerUnit*.12,0,0,Math.PI*2);ctx.fill();
  }
  ctx.drawImage(image,x-sourceRoot.x*scale,y-sourceRoot.y*scale,image.width*scale,image.height*scale);
  ctx.restore();
  return drop.state==='flying';
}
function draw(now) {
  raf=0;
  if(!ready) return;
  ctx.clearRect(0,0,canvas.width,canvas.height);
  ctx.drawImage(scene,0,0,1080,2340);
  if(radius.checked){
    ctx.save();ctx.strokeStyle='#a9e7a7';ctx.lineWidth=3;ctx.setLineDash([8,8]);ctx.beginPath();ctx.ellipse(hero.x,hero.y,pixelsPerUnit,pixelsPerUnit/2,0,0,Math.PI*2);ctx.stroke();ctx.setLineDash([]);ctx.fillStyle='#d6f6d3';ctx.font='20px system-ui';ctx.fillText('1 birim · inceleme çizgisi',hero.x-110,hero.y+84);ctx.restore();
  }
  let animating=false;
  for(const drop of [...drops].sort((a,b)=>a.y-b.y)) animating=drawDrop(drop,now)||animating;
  const t=(now-pulseAt)/180;
  if(t>=0&&t<1&&!reduceMotion.matches){ctx.save();ctx.strokeStyle=`rgba(169,227,241,${(1-t)*.65})`;ctx.lineWidth=3;ctx.beginPath();ctx.ellipse(hero.x,hero.y,15+t*15,7+t*7,0,0,Math.PI*2);ctx.stroke();ctx.restore();animating=true;}
  if(animating) requestDraw();
}
function requestDraw(){if(!raf)raf=requestAnimationFrame(draw);}
size.addEventListener('input',()=>{document.getElementById('sizeValue').value=Number(size.value).toFixed(2).replace('.',',');requestDraw();});
density.addEventListener('change',reset);
radius.addEventListener('change',requestDraw);
gray.addEventListener('change',()=>{canvas.style.filter=gray.checked?'grayscale(1)':'none';});
collect.addEventListener('click',()=>begin(false));
sweep.addEventListener('click',()=>begin(true));
document.getElementById('reset').addEventListener('click',reset);
image.src='reward-v1.png';
scene.src='../../2026-09-19/daggers-runtime-01/runtime-room.png';
Promise.all([image.decode(),scene.decode()]).then(()=>{ready=true;reset();}).catch(()=>setStatus('Görsel yüklenemedi. Önizlemeyi ArtDirection kökünden sunun.'));
updateButtons();
