// Concept from https://www.youtube.com/watch?v=aP6w2OzidYM

export declare const __brand: unique symbol;

export type Branded<T, Name extends string> = T & { [__brand]: Name };
