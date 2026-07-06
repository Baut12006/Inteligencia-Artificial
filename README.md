# Inteligencia-Artificial
Nombre del juego: "Astro Sneak"

Género: Sigilo

Objetivo: Eliminar a todos los enemigos para poder avanzar.

Sistemas de IA implementados: Se implemento una FSM para los enemigos que funciona en conjunto con un Line of Sight y un Enemy controller, la FSM maneja los disntintos estados, el line of sight es la visión del enemigo, en base a lo que ve es como actúa y el enemy controller hace que todo ese movimiento funcione, es decir coordina la lógica de movimiento y toma de decisiones.

Controles básicos: WASD para el movimiento.

CAMBIOS PARA LA SEGUNDA ENTREGA:

-Los viejos enemigos funcionan combinando los wayponts anteriores y los nuevos conceptos vistos: Ahora la FSM decide que hacer y el pathfinding resuelve por donde ir dentro del mapa esquivando obstáculos y los steering behaviours resuelven como se desplazan los enemigos.

-Los nuevos enemigos (Hunter y Scout): El Hunter patrulla como los demás, pero cuando te ve no va a tu posición actual sino que predice hacia donde te estás moviendo (Purse), así te corta el paso y es el más agresivo, mientras que el Scout no patrulla por waypoints sino que deambula de forma errática (Wander), y cuando te ve no ataca: huye. Si tiene el camino libre escapa con un steering local (Evade).

-Aunque no tenga que ver con la IA del juego, también se mejoro el Feedback visual para cuando te escondes dentro de las "Shadow Zones", ahora estas parpadean para delimitar cuanto territorio abarcan y cuando estas dentro de ellas el material del player se oscurece.


CAMBIOS PARA LA ENTREGA FINAL:

-Ahora el enemycontroller tiene la posibilidad de elegir(dependiendo del que se le pase por inspector) el tupo de cerebro, que anteriormente era solamente una FSM, ahora esta la posibilidad de usar un Decision Tree, y una nueva FSM_Classes, y también se cambio el enemy data para que se pueda elegir el tipo de algoritmo de pahtfinding (a* o theta*).

-Los dos nuevos enemigos son el Super Hunter (Que usa la fsm_classes y Theta*) funciona como el hunter solo que es más rápido y ve en la oscuridad, y Pedrito (que usa Decision Tree y A*) y funciona como un enemigo normal solo que es mucho más lento.
