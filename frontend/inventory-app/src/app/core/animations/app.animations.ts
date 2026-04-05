import {
  animate, animation, query, stagger,
  style, transition, trigger, useAnimation
} from '@angular/animations';

const easeStandard = 'cubic-bezier(0.4, 0, 0.2, 1)';
const easeSpring   = 'cubic-bezier(0.34, 1.56, 0.64, 1)';
const easeOut      = 'cubic-bezier(0, 0, 0.2, 1)';

export const fadeSlideUp = animation([
  style({ opacity: 0, transform: 'translateY(20px)' }),
  animate(`320ms ${easeStandard}`, style({ opacity: 1, transform: 'translateY(0)' }))
]);

export const pageEnter = trigger('pageEnter', [
  transition(':enter', [useAnimation(fadeSlideUp)])
]);

export const listStagger = trigger('listStagger', [
  transition('* => *', [
    query(':enter', [
      style({ opacity: 0, transform: 'translateY(18px)' }),
      stagger('45ms',
        animate(`280ms ${easeStandard}`, style({ opacity: 1, transform: 'translateY(0)' }))
      )
    ], { optional: true })
  ])
]);

export const toastSlide = trigger('toastSlide', [
  transition(':enter', [
    style({ opacity: 0, transform: 'translateX(110%) scale(0.88)' }),
    animate(`340ms ${easeSpring}`, style({ opacity: 1, transform: 'translateX(0) scale(1)' }))
  ]),
  transition(':leave', [
    animate(`200ms ${easeOut}`, style({ opacity: 0, transform: 'translateX(110%)' }))
  ])
]);

export const scaleIn = trigger('scaleIn', [
  transition(':enter', [
    style({ opacity: 0, transform: 'scale(0.92)' }),
    animate(`260ms ${easeSpring}`, style({ opacity: 1, transform: 'scale(1)' }))
  ])
]);

export const expandCollapse = trigger('expandCollapse', [
  transition(':enter', [
    style({ height: 0, opacity: 0, overflow: 'hidden' }),
    animate(`260ms ${easeStandard}`, style({ height: '*', opacity: 1 }))
  ]),
  transition(':leave', [
    style({ overflow: 'hidden' }),
    animate(`180ms ${easeOut}`, style({ height: 0, opacity: 0 }))
  ])
]);
