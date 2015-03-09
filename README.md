# no-frills-transformation

"No frills transformation" (NFT) is intended to be a super lightweight transformation engine, with very limited support for reading and writing stuff, but having an extensible interface.

Out of the box, NFT will read:

* CSV files

and write

* CSV files

because... that's what I currently need ;-)

In an ETL scenario, NFT is neither designed to do the "E" nor the "L" part, just simple "T" tasks. 
But that quickly and efficiently, supporting the basic transformation stuff you might need (and
 with extensibility support if you need something out of the order). Among supported transformations are:

* Copy (nop transformation, copy source to target)
* Lookup (in other sources)
* Filtering (on source data)

Feel free to contribute and create pull requests. I'll check them out and merge them if they make sense.
