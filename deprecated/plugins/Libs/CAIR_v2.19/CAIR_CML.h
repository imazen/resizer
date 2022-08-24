#ifndef CAIR_CML_H
#define CAIR_CML_H

//=========================================================================================================//
//CAIR Matrix Library
//Copyright (C) 2009 Joseph Auman (brain.recall@gmail.com)

//=========================================================================================================//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

//=========================================================================================================//
//This class will be used to handle and manage all matrix types used in CAIR.
//For grayscale images, use the type CML_gray.
//For standard color images, use the type CML_color.
//For energy, edges, and weights, use the type CML_int.
//This class is used to replace and consolidate the several types I had prevously maintained separately.

//Note for developers: Unfortunately, this class means that a separate translation function will need
//	to be written to translate from whatever internal image object to the CML_Matrix. This will keep CAIR
//	more flexible, but adds another small step.
//=========================================================================================================//

#include <cstring> //for memcpy(), memmove()
#include <cstdlib> //for realloc()

//CML_DEBUG will print out information to the console window when CAIR tries
// to step out-of-bounds of the matrix. For development purposes.
//#define CML_DEBUG
#ifdef CML_DEBUG
#include <iostream>
#endif

//=========================================================================================================//
template <typename T>
class CML_Matrix
{
public:
	//=========================================================================================================//
	//Simple constructor.
	CML_Matrix( int x, int y )
	{
		Allocate_Matrix( x, y );
		current_x = x;
		current_y = y;
		max_x = x;
		max_y = y;
	}
	//=========================================================================================================//
	//Simple destructor.
	~CML_Matrix()
	{
		Deallocate_Matrix();
	}

	//=========================================================================================================//
	//Assignment operator; not really fast, but it works reasonably well.
	//Does not copy Reserve()'ed memory.
	CML_Matrix& operator= ( const CML_Matrix& input )
	{
		if( this == &input ) //self-assignment check
		{
			return *this;
		}
		Deallocate_Matrix();
		Allocate_Matrix( input.current_x, input.current_y );
		current_x = input.current_x;
		current_y = input.current_y;
		max_x = current_x;
		max_y = current_y;

		for( int y = 0; y < current_y; y++ )
		{
			//ahh, memcpy(), how I love thee
			std::memcpy( &(matrix[y][0]), &(input.matrix[y][0]), current_x*sizeof(T) );
		}
		return *this;
	}

	//=========================================================================================================//
	//Set all values.
	//This is important to do since the memory is not set a value after it is allocated.
	//Make sure to do this for the weights.
	void Fill( T value )
	{
		for( int y = 0; y < CML_Matrix::Height(); y++ )
		{
			for( int x = 0; x < CML_Matrix::Width(); x++ )
			{
				matrix[y][x] = value; //remember, ROW MAJOR
			}
		}
	}

	//=========================================================================================================//
	//For the access/assignment () calls.
	inline T& operator()( int x, int y )
	{
#ifdef CML_DEBUG
		if( x<0 || x>=current_x || y<0 || y>=current_y )
		{
			std::cout << "CML_DEBUG: " << x << "," << y << std::endl;
			std::cout << "current_x=" << current_x << " current_y=" << current_y << std::endl;
		}
#endif
		return matrix[y][x]; //remember, ROW MAJOR
	}

	//Returns the current image Width.
	inline int Width()
	{
		return current_x;
	}

	//Returns the current image Height.
	inline int Height()
	{
		return current_y;
	}

	//=========================================================================================================//
	//Does a flip/rotate on Source and stores it into ourself.
	void Transpose( CML_Matrix<T> * Source )
	{
		CML_Matrix::D_Resize( (*Source).Height(), (*Source).Width() );

		for( int y = 0; y < (*Source).Height(); y++ )
		{
			for( int x = 0; x < (*Source).Width(); x++ )
			{
				matrix[x][y] = (*Source)(x,y); //remember, ROW MAJOR
			}
		}
	}

	//=========================================================================================================//
	//Mimics the EasyBMP () operation, by constraining an out-of-bounds value back into the matrix.
	inline T Get( int x, int y )
	{
		if( x < 0 )
		{
			x = 0;
		}
		else if( x >= current_x )
		{
			x = current_x - 1;
		}
		if( y < 0 )
		{
			y = 0;
		}
		else if( y >= current_y )
		{
			y = current_y - 1;
		}
		return matrix[y][x]; //remember, ROW MAJOR
	}

	//=========================================================================================================//
	//Destructive resize of the matrix.
	void D_Resize( int x, int y )
	{
		Deallocate_Matrix();
		Allocate_Matrix( x, y );
		current_x = x;
		current_y = y;
		max_x = x;
		max_y = y;
	}

	//=========================================================================================================//
	//Non-destructive resize, but only in the x direction.
	//Enlarging requires memory to be Reserve()'ed beforehand, for speed reasons.
	void Resize_Width( int x )
	{
		if( x > max_x )
		{
			//a graceful, slow, way to handle when someone screws up
			for( int i = 0; i < current_y; i++ )
			{
				matrix[i] = (T*)std::realloc( matrix[i], x*sizeof(T) );
			}
			max_x = x;
		}
		current_x = x;
	}

	//=========================================================================================================//
	//Destructive memory reservation for the internal matrix.
	//The reported size of the image does not change.
	void Reserve( int x, int y )
	{
		Deallocate_Matrix();
		Allocate_Matrix( x, y );

		max_x = x;
		max_y = y;
		//current_x and y didn't change
	}

	//=========================================================================================================//
	//Shift a row where the first element in the shift is supplied as x,y.
	//The amount/direction of the shift is supplied in shift.
	//Negative will shift left, positive right.
	void Shift_Row( int x, int y, int shift )
	{
		//Pretty much all of this stuff is really delicate, so don't touch it, unless you REALLY like to corrupt the heap...

		//special case when we don't want any shifting to occur
		if( (x >= current_x) && (shift<0) )
			return;

		//bounds check the input since it's really important
		if( x < 0 )
		{
			x = 0;
		}
		else if( x >= current_x )
		{
			x = current_x - 1;
		}
		if( y < 0 )
		{
			y = 0;
		}
		else if( y >= current_y )
		{
			y = current_y - 1;
		}

		//calculate and bounds check the destination x
		int x_shift = x + shift;
		if( x_shift < 0 )
		{
			x_shift = 0;
		}
		else if( x_shift >= current_x )
		{
			x_shift = current_x - 1;
		}

		//calculate how many elements to move
		int shift_amount = current_x - x;
		//keep us from shifting off the edge when shifting right
		if( shift > 0 )
		{
			shift_amount -= shift;
		}

		//memmove because this WILL overlap
		std::memmove( &(matrix[y][x_shift]), &(matrix[y][x]), shift_amount*sizeof(T) );
	}

private:
	//=========================================================================================================//
	//Simple row-major 2D allocation algorithm.
	//The size variables must be assigned seperately.
	void Allocate_Matrix( int x, int y )
	{
		matrix = new T*[y];

		for( int i = 0; i < y; i++ )
		{
			matrix[i] = new T[x];
		}
	}
	//Simple row-major 2D deallocation algorithm.
	//Doest not maintain size variables.
	void Deallocate_Matrix()
	{
		for( int i = 0; i < max_y; i++ )
		{
			delete[] matrix[i];
		}

		delete[] matrix;
	}

	T ** matrix;
	int current_x;
	int current_y;
	int max_x;
	int max_y;
};


//=========================================================================================================//
typedef unsigned char CML_byte;

//standard 32 bit pixel
struct CML_RGBA
{
	CML_byte red;
	CML_byte green;
	CML_byte blue;
	CML_byte alpha;
};

typedef CML_Matrix<CML_RGBA> CML_color; //use for images
typedef CML_Matrix<int> CML_int; //use for weights

#endif //CAIR_CML_H
