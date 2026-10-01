unity game project Agile from 2026 (abandoned due to scope creep). 2d platformer with mecha inspired loadout system, player would buy different limbs and
weapons to equip to their body each with different stats and physical appearances. the main feature was the body was simulated allowing different parts
to effect gameplay in a more direct manner with unique hitboxes, walking styles and interactions with the enemy.

requirements :
  - OS that can run unity editor.
  - unity installed on machine to run the editor.
  - a unity version to run the project in.

install instructions :
    - download repository and unzip.
    - open unity hub and click add then add project from disk.
    - navigate and select unzipped repository.
    - you will be prompted for a version to open it in, selecting missing version or latest LTS version will work best.
    - wait for editor to load and enjoy.

gameplay notes :

the player could move left and right(AD) and up and down(WS), they could jump(space) with jump height depending on their evalation above the floor. 

the player could swap weapons(Q) and could aim(RMB) and fire their weapon(LMB).
the player could also ragdoll(R) and de-ragdoll by pressing any key

player controller : 
  - each limb in the players body is governed by its own script, arms grab weapons and aim, the torso keeps the body above the ground and the legs step
    through the environment allowing for obstacles to be stepped over dynamically (this system in AGILE is somewhat messy, a better version exists in
    the Iconoclast project repository).

  - each limb had a series of "vanity plates" which covered the mechanical insides and gave each limb more personality, the intention was that players
    would have some ability to customise these to give them greater expression. as a limb got damaged these plates would pop off the chasis to
    show armour being depleted/damaged.

  - the goal of this system was a pseudo-active ragdoll where the players body moved in a responsive/believable manner with the potential for emergant
    gameplay such as using larger legs as shields by lowering yourself.

weapons/damage : (development stopped as this system was being finalised)
  - guns in AGILE were designed to be customisable, weapons could fire in bursts and the bullet they fire is a seperate object allowing anything to be
    a bullet if the game ever demanded it. to simplify animations and design weapons didn't reload but would overheat as they were fired until they
    were forced to cool down, this combined with a ammo pool would have allowed for any kind of gun I would have wanted to add in the finished product.

  - each limb segment could be individually damaged with limbs slowly losing functionality until they ragdoll or fall off the body entirely, damage
    was calculated by taking the AP and comparing against the armour of the segment, low AP meant no damage would be dealt while higher AP would make
    more of your max damage be applied.
    
  - when a segment was hit its armor value would decrease with low AP weapons being the best for this cause, giving them a potential use against
    more highly armoured foes that would have featured in the full game.

please direct all inquiries, questions and problems to ruled.dev@gmail.com
