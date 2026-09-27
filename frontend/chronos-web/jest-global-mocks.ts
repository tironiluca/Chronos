import { jest } from '@jest/globals';

Object.defineProperty(document, 'doctype', {
  value: '<!DOCTYPE html>'
});

Object.defineProperty(window, 'getComputedStyle', {
  value: () => ({
    display: 'none',
    appearance: ['-webkit-appearance']
  })
});

// Workaround for jsdom missing the transform property (angular/material2#7101).
Object.defineProperty(document.body.style, 'transform', {
  value: () => ({
    enumerable: true,
    configurable: true
  })
});

HTMLCanvasElement.prototype.getContext = jest.fn() as typeof HTMLCanvasElement.prototype.getContext;
