/* The Dark Quiet iPhone v5 — atmosphere, depth, world dressing */
(function(){
const oldGround=ground,oldClutter=clutter,oldPlayer=player,oldWeather=weather,oldHud=hud;
function rr(x,y,w,h,c){R(x,y,w,h,c)}
function shrub(x,y,s=1){let q=iso(x,y);for(let i=0;i<7;i++){let dx=((i*13)%21-10)*s,dy=((i*7)%11)*s;rr(q[0]+dx-5*s,q[1]-13*s+dy,11*s,8*s,i%3?'#263c24':'#34472a')}}
function bin(x,y){let q=iso(x,y);rr(q[0]-6,q[1]-14,12,15,'#28312e');rr(q[0]-7,q[1]-16,14,3,'#424b47')}
function bollard(x,y){let q=iso(x,y);rr(q[0]-2,q[1]-12,4,13,'#4b514e');rr(q[0]-3,q[1]-13,6,3,'#272d2b')}
function planter(x,y){let q=iso(x,y);rr(q[0]-12,q[1]-9,24,11,'#555047');rr(q[0]-9,q[1]-14,18,6,'#1c3020');for(let i=0;i<4;i++)rr(q[0]-7+i*5,q[1]-20-(i&1)*3,3,9,'#3f5832')}
function cone(x,y){let q=iso(x,y);g.fillStyle='#a55b32';g.beginPath();g.moveTo(q[0],q[1]-16);g.lineTo(q[0]-7,q[1]);g.lineTo(q[0]+7,q[1]);g.closePath();g.fill();rr(q[0]-9,q[1],18,3,'#4b4037');rr(q[0]-4,q[1]-7,8,2,'#c9b681')}
function drain(x,y){let q=iso(x,y);rr(q[0]-10,q[1]-3,20,6,'#161c1b');for(let i=-7;i<9;i+=4)rr(q[0]+i,q[1]-2,1,4,'#59605c')}
function cracks(){g.strokeStyle='#12191799';g.lineWidth=1;for(const a of[[-6,3,34],[-2,4,24],[4,4,31],[7,3,20]]){let q=iso(a[0],a[1]);g.beginPath();g.moveTo(q[0]-a[2]/2,q[1]);g.lineTo(q[0]-3,q[1]+3);g.lineTo(q[0]+6,q[1]-2);g.lineTo(q[0]+a[2]/2,q[1]+1);g.stroke()}}
function wetReflections(){g.globalCompositeOperation='screen';for(const a of[[-4,.7],[.5,.5],[5,.8],[8,-2]]){let q=iso(a[0],a[1]);let gr=g.createLinearGradient(q[0],q[1]-4,q[0],q[1]+60);gr.addColorStop(0,'#f1bd5d22');gr.addColorStop(1,'#f1bd5d00');g.fillStyle=gr;g.beginPath();g.moveTo(q[0]-7,q[1]);g.lineTo(q[0]+7,q[1]);g.lineTo(q[0]+24,q[1]+58);g.lineTo(q[0]-24,q[1]+58);g.closePath();g.fill()}g.globalCompositeOperation='source-over'}
ground=function(){oldGround();cracks();for(const a of[[-7,4.1],[-1,5.1],[5,5.4],[9,3.5]])drain(...a)};
clutter=function(){oldClutter();for(const a of[[-7.2,-.5],[-5.8,-.3],[2.9,-.8],[4,-1],[7.8,-1.2]])shrub(...a);planter(-1.4,-.5);planter(.2,-.7);bin(-3.5,.2);bin(7,-1);bollard(-.5,.2);bollard(.1,.35);bollard(.7,.5);cone(5.7,3.2);cone(6.2,3.1);let q=iso(2.2,2.4);rr(q[0]-12,q[1]-4,24,8,'#40382f');rr(q[0]-8,q[1]-7,5,4,'#d2c5a0');};
player=function(){let q=iso(p.x,p.y);g.fillStyle='#05080766';g.beginPath();g.ellipse(q[0],q[1]+2,12,5,0,0,Math.PI*2);g.fill();oldPlayer();rr(q[0]-8,q[1]-29,5,15,'#253d38');rr(q[0]+3,q[1]-27,7,13,'#263b36');rr(q[0]-9,q[1]-25,2,10,'#66726c')};
weather=function(){oldWeather();wetReflections();let v=g.createRadialGradient(W*.5,H*.48,Math.min(W,H)*.22,W*.5,H*.48,Math.max(W,H)*.72);v.addColorStop(0,'#00000000');v.addColorStop(.7,'#00000018');v.addColorStop(1,'#00000099');g.fillStyle=v;g.fillRect(0,0,W,H);};
hud=function(){oldHud();if(!inside&&!bag){g.fillStyle='#d6d8d0aa';g.font='8px monospace';g.fillText('SKOLGÅRDEN',20,82)}};
})();