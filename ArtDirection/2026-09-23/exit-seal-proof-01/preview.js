'use strict';
const canvas=document.querySelector('#arena'),ctx=canvas.getContext('2d'),scene=new Image();
const state=document.querySelector('#state'),hold=document.querySelector('#hold'),status=document.querySelector('#status');
let ready=false,running=false,paused=false,last=0,raf=0;
const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches;
// Reference camera width9 ->120px/world unit; floor Y projects at half scale. This is not a live camera binding.
const cx=540,cy=900,r=0.9*120;
function draw(){
 if(!ready)return;ctx.clearRect(0,0,1080,2340);ctx.drawImage(scene,0,0,1080,2340);
 const visible=!['ended','descent'].includes(state.value),v=state.value==='holding'?Number(hold.value):0;
 document.querySelector('#value').value=v.toFixed(2).replace('.',',')+' s';
 status.textContent=({idle:'Çıkışa yürü.',moving:'Çıkmak için alanın içinde hareketsiz dur.',holding:v>=1?'Örnek süre doldu. Gerçek oyunda sonucu oyun kuralı belirler.':'Hareketsiz duruş: '+Math.round(v*100)+'%',ended:'Oda sona erdi; çıkış çizimi gizli.',descent:'Descent odasında çıkış mührü gösterilmez.'})[state.value];
 if(!visible)return;
 ctx.save();ctx.translate(cx,cy);ctx.scale(1,.5);
 ctx.strokeStyle='#141b24';ctx.lineWidth=11;ctx.beginPath();ctx.arc(0,0,r,0,Math.PI*2);ctx.stroke();
 ctx.strokeStyle='#b39560';ctx.lineWidth=4;ctx.beginPath();ctx.arc(0,0,r,0,Math.PI*2);ctx.stroke();
 // The circle defines reach. Four angular brass marks and the gateway define identity, independently of that circle.
 for(let i=0;i<4;i++){ctx.save();ctx.rotate(Math.PI/4+i*Math.PI/2);ctx.strokeStyle='#e0be7e';ctx.lineWidth=7;ctx.beginPath();ctx.moveTo(-17,-78);ctx.lineTo(-17,-91);ctx.lineTo(17,-91);ctx.lineTo(17,-78);ctx.stroke();ctx.restore();}
 ctx.strokeStyle='#c8e9dc';ctx.lineWidth=7;ctx.lineJoin='miter';ctx.beginPath();ctx.moveTo(-28,35);ctx.lineTo(-28,-31);ctx.lineTo(28,-31);ctx.lineTo(28,35);ctx.moveTo(-39,35);ctx.lineTo(39,35);ctx.stroke();
 ctx.lineWidth=6;ctx.beginPath();ctx.moveTo(0,22);ctx.lineTo(0,-14);ctx.moveTo(-12,-2);ctx.lineTo(0,-15);ctx.lineTo(12,-2);ctx.stroke();
 if(v>0){ctx.strokeStyle='#93f2d1';ctx.lineWidth=8;ctx.beginPath();ctx.arc(0,0,r,-Math.PI/2,-Math.PI/2+Math.PI*2*v);ctx.stroke();}
 ctx.restore();ctx.fillStyle='#e1eee7';ctx.font='600 23px system-ui';ctx.textAlign='center';ctx.fillText(state.value==='moving'?'HAREKETSİZ DUR':state.value==='holding'?'ÇIKIŞ · '+Math.round(v*100)+'%':'ÇIKIŞ',cx,cy+90);
}
function tick(t){raf=0;if(!running||paused)return;hold.value=Math.min(1,Number(hold.value)+(t-last)/1000);last=t;draw();if(Number(hold.value)<1)raf=requestAnimationFrame(tick);else running=false;}
function stop(){running=false;cancelAnimationFrame(raf);}
state.onchange=()=>{stop();hold.value=0;draw();};hold.oninput=()=>{stop();state.value='holding';draw();};
document.querySelector('#play').onclick=()=>{stop();paused=false;document.querySelector('#pause').textContent='Örneği duraklat';state.value='holding';hold.value=reduced?1:0;draw();if(!reduced){running=true;last=performance.now();raf=requestAnimationFrame(tick);}};
document.querySelector('#interrupt').onclick=()=>{stop();state.value='moving';hold.value=0;draw();};
document.querySelector('#pause').onclick=e=>{paused=!paused;e.target.textContent=paused?'Örneği sürdür':'Örneği duraklat';if(!paused&&running){last=performance.now();raf=requestAnimationFrame(tick);}};
document.querySelector('#gray').onchange=e=>canvas.style.filter=e.target.checked?'grayscale(1)':'none';
scene.src='../../2026-09-19/daggers-runtime-01/runtime-room.png';scene.decode().then(()=>{ready=true;draw();}).catch(()=>status.textContent='Arka plan yüklenemedi.');
